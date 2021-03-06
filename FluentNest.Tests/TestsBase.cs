﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Elasticsearch.Net;
using FluentNest.Tests.Model;
using Nest;
using NFluent;

namespace Tests
{
    public class TestsBase
    {
        protected ElasticClient Client;

        private readonly Dictionary<string, string> testResults;

        public TestsBase(Func<ConnectionSettings, IElasticsearchSerializer> serializerFactory = null, Func<ConnectionSettings, ConnectionSettings> additionalSettings = null)
        {
            var node = new Uri("http://localhost:9200");
            var connectionPool = new SingleNodeConnectionPool(node);

            var settings = new ConnectionSettings(connectionPool, serializerFactory).DefaultIndex("fluentnesttests");
            if (additionalSettings != null)
            {
                settings = additionalSettings(settings);
            }

            Client = new ElasticClient(settings);

            testResults = LoadTestResults(this.GetType().Name);
        }

        public string Serialize<T>(T entity)
        {
            using (var ms = new MemoryStream())
            {
                Client.Serializer.Serialize(entity, ms);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public Dictionary<string, string> LoadTestResults(string className)
        {
            var fileName = className + ".txt";
            if (!File.Exists(fileName))
            {
                return new Dictionary<string, string>();
            }

            var testLines  = File.ReadAllText(className + ".txt").Split("###".ToCharArray()).Select(x=>x.Trim());
            var values = testLines.Where(x=>x.Contains("***")).Select(x=>x.Trim()).Select(x =>
            {
                var testContent = x.Split("***".ToCharArray()).Where(y => !string.IsNullOrWhiteSpace(y)).ToArray();
                return new
                {
                    Name = testContent[0].Trim(),
                    Json = testContent[1].Trim()
                };
            });

            return values.ToDictionary(x => x.Name, y => y.Json);
        }

        public void CheckSD<T>(SearchDescriptor<T> sc, string testName) where T: class
        {
            var json = Serialize(sc);
            var escaped = string.Join("", json.Where(c => !char.IsWhiteSpace(c)));

            var expected = testResults[testName];
            var escapedExpected = string.Join("", expected.Where(c => !char.IsWhiteSpace(c)));

            Check.That(escaped).Equals(escapedExpected);
        } 
    }
}
