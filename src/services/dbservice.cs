using System;
using System.Collections.Generic;
using System.Linq;
using merlin.classes;
using Microsoft.Extensions.DependencyInjection;
using LiteDB;

namespace merlin.services
{
    public class DbService
    {
        private readonly LiteDatabase _db;

        public DbService(LiteDatabase db)
        {
            _db = db;
        }

        public BsonValue AddApiKey(string service, string key) => _db.GetCollection<ApiKey>("apikeys").Insert(new ApiKey { Service = service, Key = key });

        public string GetApiKey(string service) => _db
            .GetCollection<ApiKey>("apikeys")
            .FindOne(Query
                .Where("Service", 
                    x => x.AsString == service)
            )?
            .Key;
        
        public int RemoveApiKey(string service) => _db
            .GetCollection<ApiKey>("apikeys")
            .Delete(Query
                .Where("Service", 
                    x => x.AsString == service
                )
            );

        public bool AddTag(string key, string value, ulong gId)
        {
            var collection = _db.GetCollection<Tag>("tags");
            if (GetTag(key, gId) is null)
                return collection.Upsert(new Tag { Key = key, Value = value, GuildId = gId });
            else return false;
        }

        public bool AddPrefix(string value, ulong gId)
        {
            var collection = _db.GetCollection<GuildPrefix>("prefixes");
            if (GetPrefix(gId) is null)
                return collection.Upsert(new GuildPrefix { GuildId = gId, Prefix = value });
            else return false;
        }

        public bool AddAction(ModerationAction action) => _db
            .GetCollection<ModerationAction>("actions")
            .Upsert(action);

        public int RemoveAction(ModerationAction action) => _db
            .GetCollection<ModerationAction>("actions")
                .Delete(Query
                    .And(
                        Query.EQ("GuildId", action.GuildId), 
                        Query.EQ("ModeratorId", action.ModeratorId), 
                        Query.EQ("UserId", action.UserId), 
                        Query.EQ("Type", (int)action.Type)
                    )
                );
        
        public List<ModerationAction> LoadActions() => _db
            .GetCollection<ModerationAction>("actions")
            .FindAll()
            .ToList();

        public int RemoveTag(string key, ulong gId) => _db
            .GetCollection<Tag>("tags")
            .Delete(Query
                .And(
                    Query.Where("GuildId", x => x.AsDouble == gId), 
                    Query.Where("Key", x => x.AsString.ToLower() == key.ToLower())
                )
            );
    

        public Tag GetTag(string key, ulong gId) => _db
            .GetCollection<Tag>("tags")
            .FindOne(Query
                .And(
                    Query.Where("GuildId", x => x.AsDouble == gId), 
                    Query.Where("Key", x => x.AsString.ToLower() == key.ToLower())
                )
            );
        
        public IEnumerable<string> GetTags(ulong gId) => _db
            .GetCollection<Tag>("tags")
            .Find(Query
                .Where("GuildId", x => x.AsDouble == gId)
            )
            .Select(x => x.Key);

        public GuildPrefix GetPrefix(ulong gId) => _db
            .GetCollection<GuildPrefix>("prefixes")
            .FindOne(Query
                .Where("GuildId", x => x.AsDouble == gId)
            );

        public int RemovePrefix(ulong gId) => _db
            .GetCollection<GuildPrefix>("prefixes")
            .Delete(Query
                .Where("GuildId", x => x.AsDouble == gId)
            );

        public bool AddHost(string name, string url) => _db
            .GetCollection<Host>("hosts")
            .Upsert(new Host { Name = name, Url = url });

        public bool AddHost(Host host) => _db.GetCollection<Host>("hosts").Upsert(host);

        public string GetHost(string name) => _db
            .GetCollection<Host>("hosts")
            .FindOne(Query
                .Where(
                    "Name", x => x.AsString == name
                )
            )?
            .Url;

        public IEnumerable<Object> GetItems(string collection, Query query) => _db.GetCollection(collection).Find(query);
        
        public T GetItem<T>(string collection, Query query) => _db.GetCollection<T>(collection).FindOne(query);

        public bool AddItem<T>(string collection, T item) => _db.GetCollection<T>(collection).Upsert(item);

        public int AddItems<T>(string collection, IEnumerable<T> item) => _db.GetCollection<T>(collection).InsertBulk(item);

        public int RemoveItems<T>(string collection, Query query) => _db.GetCollection<T>(collection).Delete(query);
    }
}