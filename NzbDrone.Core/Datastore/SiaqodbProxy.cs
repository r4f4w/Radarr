﻿using System;
using System.Collections.Generic;
using System.Linq;
using Sqo;

namespace NzbDrone.Core.Datastore
{
    public interface IObjectDatabase : IDisposable
    {
        IEnumerable<T> AsQueryable<T>();
        T Insert<T>(T obj) where T : ModelBase;
        T Update<T>(T obj) where T : ModelBase;
        IList<T> InsertMany<T>(IList<T> objects) where T : ModelBase;
        IList<T> UpdateMany<T>(IList<T> objects) where T : ModelBase;
        void Delete<T>(T obj) where T : ModelBase;
        void DeleteMany<T>(IEnumerable<T> objects) where T : ModelBase;
    }

    public class SiaqodbProxy : IObjectDatabase
    {
        private readonly Siaqodb _db;

        public SiaqodbProxy(Siaqodb db)
        {
            _db = db;
        }

        public void Dispose()
        {

        }

        public IEnumerable<T> AsQueryable<T>()
        {
            return _db.Cast<T>();
        }

        public T Insert<T>(T obj) where T : ModelBase
        {
            if (obj.Id != 0)
            {
                throw new InvalidOperationException("Attempted to insert object with existing ID as new object");
            }

            _db.StoreObject(obj);
            return obj;
        }

        public T Update<T>(T obj) where T : ModelBase
        {
            if (obj.Id == 0)
            {
                throw new InvalidOperationException("Attempted to update object without an ID");
            }

            _db.StoreObject(obj);
            return obj;
        }

        public IList<T> InsertMany<T>(IList<T> objects) where T : ModelBase
        {
            return DoMany(objects, Insert);
        }

        public IList<T> UpdateMany<T>(IList<T> objects) where T : ModelBase
        {
            return DoMany(objects, Update);

        }

        public void Delete<T>(T obj) where T : ModelBase
        {
            _db.Delete(obj);
        }

        public void DeleteMany<T>(IEnumerable<T> objects) where T : ModelBase
        {
            foreach (var o in objects)
            {
                Delete(o);
            }
        }

        private IList<T> DoMany<T>(IEnumerable<T> objects, Func<T, T> function) where T : ModelBase
        {
            return objects.Select(function).ToList();
        }

    }
}
