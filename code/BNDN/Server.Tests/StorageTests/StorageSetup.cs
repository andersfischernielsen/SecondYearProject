﻿using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Moq;

namespace Server.Tests.StorageTests
{
    internal class FakeDbSet<TEntity> where TEntity : class
    {
        readonly List<TEntity> _data;
        private readonly IQueryable<TEntity> _queryable; 

        public DbSet<TEntity> Object { get; private set; }

        public FakeDbSet(List<TEntity> data)
        {
            _data = data;
            _queryable = data.AsQueryable();

            // Code to get all of the async stuff to work.

            var eventStateMockSet = new Mock<DbSet<TEntity>>();
            eventStateMockSet.As<IDbAsyncEnumerable<TEntity>>()
                .Setup(m => m.GetAsyncEnumerator())
                .Returns(new TestDbAsyncEnumerator<TEntity>(_queryable.GetEnumerator()));

            eventStateMockSet.As<IQueryable<TEntity>>()
                .Setup(m => m.Provider)
                .Returns(new TestDbAsyncQueryProvider<TEntity>(_queryable.Provider));

            eventStateMockSet.As<IQueryable<TEntity>>().Setup(m => m.Expression).Returns(_queryable.Expression);
            eventStateMockSet.As<IQueryable<TEntity>>().Setup(m => m.ElementType).Returns(_queryable.ElementType);
            eventStateMockSet.As<IQueryable<TEntity>>().Setup(m => m.GetEnumerator()).Returns(_queryable.GetEnumerator());

            Object = eventStateMockSet.Object;
        }

        public TEntity Add(TEntity entity)
        {
            _data.Add(entity);
            return entity;
        }
        internal class TestDbAsyncQueryProvider<TEntity1> : IDbAsyncQueryProvider
        {
            private readonly IQueryProvider _inner;

            internal TestDbAsyncQueryProvider(IQueryProvider inner)
            {
                _inner = inner;
            }

            public IQueryable CreateQuery(Expression expression)
            {
                return new TestDbAsyncEnumerable<TEntity1>(expression);
            }

            public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            {
                return new TestDbAsyncEnumerable<TElement>(expression);
            }

            public object Execute(Expression expression)
            {
                return _inner.Execute(expression);
            }

            public TResult Execute<TResult>(Expression expression)
            {
                return _inner.Execute<TResult>(expression);
            }

            public Task<object> ExecuteAsync(Expression expression, CancellationToken cancellationToken)
            {
                return Task.FromResult(Execute(expression));
            }

            public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
            {
                return Task.FromResult(Execute<TResult>(expression));
            }
        }

        private class TestDbAsyncEnumerable<T> : EnumerableQuery<T>, IDbAsyncEnumerable<T>, IQueryable<T>
        {
            public TestDbAsyncEnumerable(Expression expression)
                : base(expression)
            { }

            public IDbAsyncEnumerator<T> GetAsyncEnumerator()
            {
                return new TestDbAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
            }

            IDbAsyncEnumerator IDbAsyncEnumerable.GetAsyncEnumerator()
            {
                return GetAsyncEnumerator();
            }

            IQueryProvider IQueryable.Provider
            {
                get { return new TestDbAsyncQueryProvider<T>(this); }
            }
        }

        private class TestDbAsyncEnumerator<T> : IDbAsyncEnumerator<T>
        {
            private readonly IEnumerator<T> _inner;

            public TestDbAsyncEnumerator(IEnumerator<T> inner)
            {
                _inner = inner;
            }

            public void Dispose()
            {
                _inner.Dispose();
            }

            public Task<bool> MoveNextAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(_inner.MoveNext());
            }

            public T Current
            {
                get { return _inner.Current; }
            }

            object IDbAsyncEnumerator.Current
            {
                get { return Current; }
            }
        }
    }
}