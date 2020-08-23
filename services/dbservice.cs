using System;
using System.Collections.Generic;
using System.Linq;
using donniebot.classes;
using Microsoft.Extensions.DependencyInjection;
using LiteDB;

namespace donniebot.services
{
    public class DbService
    {
        private readonly IServiceProvider _services;
        private const string defaultPrefix = "don.";

        public DbService(IServiceProvider services)
        {
            _services = services;
        }

        public BsonValue AddApiKey(string service, string key)
        {
            using (var scope = _services.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<LiteDatabase>();
                var collection = _db.GetCollection<ApiKey>("apikeys");
                return collection.Insert(new ApiKey { Service = service, Key = key });
            }
        }
        public string GetApiKey(string service)
        {
            using (var scope = _services.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<LiteDatabase>();
                var collection = _db.GetCollection<ApiKey>("apikeys");
                return collection.FindOne(Query.Where("Service", x => x.AsString == service)).Key;
            }
        }

        public bool AddTag(string key, string value, ulong gId)
        {
            using (var scope = _services.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<LiteDatabase>();
                var collection = _db.GetCollection<Tag>("tags");
                return collection.Upsert(new Tag { Key = key, Value = value, GuildId = gId });
            }
        }

        public int RemoveTag(string key, ulong gId)
        {
            using (var scope = _services.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<LiteDatabase>();
                var collection = _db.GetCollection<Tag>("tags");
                return collection.Delete(Query.And(Query.Where("GuildId", x => x.AsDouble == gId), Query.Where("Key", x => x.AsString.ToLower() == key.ToLower())));
            }
        }

        public Tag GetTag(string key, ulong gId)
        {
            using (var scope = _services.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<LiteDatabase>();
                var collection = _db.GetCollection<Tag>("tags");
                return collection.FindOne(Query.And(Query.Where("GuildId", x => x.AsDouble == gId), Query.Where("Key", x => x.AsString.ToLower() == key.ToLower())));
            }
        }
        public IEnumerable<string> GetTags(ulong gId)
        {
            using (var scope = _services.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<LiteDatabase>();
                var collection = _db.GetCollection<Tag>("tags");
                return collection.Find(Query.Where("GuildId", x => x.AsDouble == gId)).Select(x => x.Key);
            }
        }

        public bool IsIn<T>(string collection, Query query, out IEnumerable<T> items)
        {
            using (var scope = _services.CreateScope())
            {
                items = scope.ServiceProvider.GetRequiredService<LiteDatabase>().GetCollection<T>(collection).Find(query);
                return items.Count() > 0;
            }
        }

        public IEnumerable<object> GetItems(string collection, Query query)
        {
            using (var scope = _services.CreateScope())
                return scope.ServiceProvider.GetRequiredService<LiteDatabase>().GetCollection(collection).Find(query);
        }
        public object GetItem(string collection, Query query)
        {
            using (var scope = _services.CreateScope())
                return scope.ServiceProvider.GetRequiredService<LiteDatabase>().GetCollection(collection).FindOne(query);
        }

        public bool AddItem<T>(string collection, T item)
        {
            using (var scope = _services.CreateScope())
                return scope.ServiceProvider.GetRequiredService<LiteDatabase>().GetCollection<T>(collection).Upsert(item);
        }
        public int AddItems<T>(string collection, IEnumerable<T> item)
        {
            using (var scope = _services.CreateScope())
                return scope.ServiceProvider.GetRequiredService<LiteDatabase>().GetCollection<T>(collection).InsertBulk(item);
        }

        public int RemoveItems<T>(string collection, Query query)
        {
            using (var scope = _services.CreateScope())
                return scope.ServiceProvider.GetRequiredService<LiteDatabase>().GetCollection<T>(collection).Delete(query);
        }
    }
}