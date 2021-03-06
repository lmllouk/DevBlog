﻿using DevBlog.Models;
using Nest;
using System;

namespace DevBlog.Services.Elasticsearch
{
    public class ElasticClient
    {
        private const string _indexName = "blog";
        public static IElasticClient _elastic { get; set; }

        public static void Initialize()
        {
            _elastic = GetClient();

            // Creating the index
            _elastic.CreateIndex(_indexName, i => i
                .Settings(s => s
                    .Setting("number_of_shards", 1)
                    .Setting("number_of_replicas", 0)
                    .Analysis(a => 
                        a.Analyzers(x => x.Language("Bulgarian", l => l.Language(Language.Bulgarian))))));

            // Creating the types
            _elastic.Map<PostType>(x => x
                .Index(_indexName)
                .AutoMap());
        }

        private static IElasticClient GetClient()
        {
            var urlString = new Uri("http://localhost:9200");
            var settings = new ConnectionSettings(urlString).DisableDirectStreaming();

            return new Nest.ElasticClient(settings);
        }

        public void Delete(int id)
        {
            _elastic.Delete<PostType>(id, i => i.Index(_indexName));
        }

        public void Index(PostType document)
        {
            _elastic.Index(document, i => i.Id(document.Id.ToString())
                .Index(_indexName)
                .Type<PostType>());
        }

        public ISearchResponse<PostType> Search(string terms)
        {
            var result = _elastic.Search<PostType>(s => s
                .Index(_indexName)
                .Type<PostType>()
                .Query(q => BuildQuery(terms)));

            return result;
        }

        private QueryContainer BuildQuery(string terms)
        {
            QueryContainer query = Query<PostType>.MultiMatch(mm => mm
                    .Query(terms)
                    .Type(TextQueryType.MostFields)
                    .Fields(f => f
                        .Field(ff => ff.Title, boost: 3)
                        .Field(ff => ff.Tags, boost: 2)
                        .Field(ff => ff.Content, boost: 1))); 

            return query;
        }
    }
}
