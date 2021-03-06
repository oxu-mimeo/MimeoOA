﻿using Abp.Domain.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Abp.EntityFrameworkCore.Uow
{
    public class EfCoreUnitOfWork : UnitOfWorkBase
    {
        private readonly IDbContextResolver dbContextResolver;
        protected IDictionary<string, DbContext> ActiveDbContexts { get; private set; }
        protected IDbContextTransaction SharedTransaction;
        public EfCoreUnitOfWork(IUnitOfWorkDefaultOptions defaultOptions, IDbContextResolver dbContextResolver) : base(defaultOptions)
        {
            this.dbContextResolver = dbContextResolver;
            ActiveDbContexts = new Dictionary<string, DbContext>();
            Completed += EfCoreUnitOfWork_Completed;
            Disposed += EfCoreUnitOfWork_Disposed;
        }

        private void EfCoreUnitOfWork_Disposed(object sender, EventArgs e)
        {
           //Dispose the thrid plug or other instance
        }

        private void EfCoreUnitOfWork_Completed(object sender, EventArgs e)
        {
           //It need to send email or do other thing.
        }
        public override void SaveChanges()
        {
            //foreach (var dbContext in ActiveDbContexts.Values)
            //{
            //    SaveChangesInDbContext(dbContext);
            //}
            SaveChangesInDbContext(this.dbContextResolver.Resolve(DBSelector.Master));
        }

        public override async Task SaveChangesAsync()
        {
            //foreach (var dbContext in ActiveDbContexts.Values)
            //{
            //    await SaveChangesInDbContextAsync(dbContext);
            //}

            await SaveChangesInDbContextAsync(this.dbContextResolver.Resolve(DBSelector.Master));
        }

        protected override void BeginUow()
        {
            //GetOrCreateDbcontext<DbContext>();
            if (Options.IsTransactional==true)
            {
                BeginTransaction(this.dbContextResolver.Resolve(DBSelector.Master));
            }
            base.BeginUow();
        }

        public virtual TDbContext GetOrCreateDbcontext<TDbContext>() where TDbContext : DbContext
        {
            var dbConnectDbContextType = typeof(TDbContext);
            var dbContextKey = $"{dbConnectDbContextType.FullName}#";

            DbContext dbContext;

            if (!ActiveDbContexts.TryGetValue(dbContextKey, out dbContext))
            {

                dbContext = this.dbContextResolver.Resolve(DBSelector.Master);
                if (Options.IsTransactional == true)
                {
                    BeginTransaction(dbContext);
                }
                //ActiveDbContexts[dbContextKey] = dbContext;
            }
            return (TDbContext)dbContext;
        }

        protected override void CompleteUow()
        {
            SaveChanges();
            CommitTransaction();
        }

        protected override async Task CompleteUowAsync()
        {
            await SaveChangesAsync();
            CommitTransaction();
        }

        protected override void DisposeUow()
        {
            ActiveDbContexts.Clear();
        }

        private void BeginTransaction(DbContext dbContext)
        {
            dbContext.Database.BeginTransaction();
        }
        private void CommitTransaction()
        {
            if (Options.IsTransactional != true)
            {
                return;
            }
            //foreach (var dbContext in ActiveDbContexts.Values)
            //{
            //    dbContext.Database.CommitTransaction();
            //}
            var dbContext = this.dbContextResolver.Resolve(DBSelector.Master);
            dbContext.Database.CommitTransaction();
        }
        protected virtual void SaveChangesInDbContext(DbContext dbContext)
        {
            dbContext.SaveChanges();
        }

        protected virtual async Task SaveChangesInDbContextAsync(DbContext dbContext)
        {
            await dbContext.SaveChangesAsync();
        }

        protected virtual void Release(DbContext dbContext)
        {
            dbContext.Dispose();
            dbContext.Database.CurrentTransaction.Dispose();
        }
    }
}
