using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using donniebot.classes;
using Discord.WebSocket;
using System.IO;
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