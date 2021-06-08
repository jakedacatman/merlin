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

                var apiKey = collection.FindOne(Query.Where("Service", x => x.AsString == service));
                return apiKey?.Key;
            }
        }

        public bool AddTag(string key, string value, ulong gId)
        {
            using (var scope = _services.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<LiteDatabase>();
                var collection = _db.GetCollection<Tag>("tags");
                if (GetTag(key, gId) == null)
                    return collection.Upsert(new Tag { Key = key, Value = value, GuildId = gId });
                else return false;
            }
        }

        public bool AddPrefix(string value, ulong gId)
        {
            using (var scope = _services.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<LiteDatabase>();
                var collection = _db.GetCollection<GuildPrefix>("prefixes");
                if (GetPrefix(gId) == null)
                    return collection.Upsert(new GuildPrefix { GuildId = gId, Prefix = value });
                else return false;
            }
        }

        public bool AddAction(ModerationAction action)
        {
            using (var scope = _services.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<LiteDatabase>();
                var collection = _db.GetCollection<ModerationAction>("actions");
                return collection.Upsert(action);
            }
        }
        public int RemoveAction(ModerationAction action)
        {
            using (var scope = _services.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<LiteDatabase>();
                var collection = _db.GetCollection<ModerationAction>("actions");
                return collection.Delete(Query.And(Query.EQ("GuildId", action.GuildId), Query.EQ("ModeratorId", action.ModeratorId), Query.EQ("UserId", action.UserId), Query.EQ("Type", (int)action.Type)));
            }
        }
        
        public List<ModerationAction> LoadActions()
        {
            using (var scope = _services.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<LiteDatabase>();
                var collection = _db.GetCollection<ModerationAction>("actions");
                return collection.FindAll().ToList();
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

        public GuildPrefix GetPrefix(ulong gId)
        {
            using (var scope = _services.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<LiteDatabase>();
                var collection = _db.GetCollection<GuildPrefix>("prefixes");
                return collection.FindOne(Query.Where("GuildId", x => x.AsDouble == gId));
            }
        }
        public int RemovePrefix(ulong gId)
        {
            using (var scope = _services.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<LiteDatabase>();
                var collection = _db.GetCollection<GuildPrefix>("prefixes");
                return collection.Delete(Query.Where("GuildId", x => x.AsDouble == gId));
            }
        }

        public bool AddHost(string name, string url)
        {
            using (var scope = _services.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<LiteDatabase>();
                var collection = _db.GetCollection<Host>("hosts");
                return collection.Upsert(new Host { Name = name, Url = url });
            }
        }
        public bool AddHost(Host host)
        {
            using (var scope = _services.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<LiteDatabase>();
                var collection = _db.GetCollection<Host>("hosts");
                return collection.Upsert(host);
            }
        }
        public string GetHost(string name)
        {
            using (var scope = _services.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<LiteDatabase>();
                var collection = _db.GetCollection<Host>("hosts");
                var host = collection.FindOne(Query.Where("Name", x => x.AsString == name));
                return host?.Url;
            }
        }

        public IEnumerable<Object> GetItems(string collection, Query query)
        {
            using (var scope = _services.CreateScope())
                return scope.ServiceProvider.GetRequiredService<LiteDatabase>().GetCollection(collection).Find(query);
        }
        public T GetItem<T>(string collection, Query query)
        {
            using (var scope = _services.CreateScope())
                return scope.ServiceProvider.GetRequiredService<LiteDatabase>().GetCollection<T>(collection).FindOne(query);
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