using Data.Models;
using DATA.DataAccess.Context;
using DATA.DataAccess.Repositories;
using DATA.DataAccess.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore; // Added for CreateExecutionStrategy
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DataAccess.Repositories.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UnitOfWork> _logger;
        private IDbContextTransaction _transaction;

        public IBaseRepository<AppUser> AppUsers { get; private set; }
        public IBaseRepository<Product> Products { get; private set; }
        public IBaseRepository<ProductImage> ProductImages { get; private set; }
        public IBaseRepository<Cart> Carts { get; private set; }
        public IBaseRepository<CartItem> CartItems { get; private set; }
        public IBaseRepository<Order> Orders { get; private set; }
        public IBaseRepository<OrderItem> OrderItems { get; private set; }
        public IBaseRepository<Invoice> Invoices { get; private set; }
        public IBaseRepository<ReturnRequest> ReturnRequests { get; private set; }
        public IBaseRepository<ReturnItem> ReturnItems { get; private set; }

        public UnitOfWork(AppDbContext context, ILogger<UnitOfWork> logger)
        {
            _context = context;
            _logger = logger;

            AppUsers = new BaseRepository<AppUser>(_context);
            Products = new BaseRepository<Product>(_context);
            ProductImages = new BaseRepository<ProductImage>(_context);
            Carts = new BaseRepository<Cart>(_context);
            CartItems = new BaseRepository<CartItem>(_context);
            Orders = new BaseRepository<Order>(_context);
            OrderItems = new BaseRepository<OrderItem>(_context);
            Invoices = new BaseRepository<Invoice>(_context);
            ReturnRequests = new BaseRepository<ReturnRequest>(_context);
            ReturnItems = new BaseRepository<ReturnItem>(_context);
        }

        public async Task<int> CommitAsync()
        {
            try
            {
                return await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while committing changes");
            }
            return 0;
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }
        
        public async Task CommitTransactionAsync()
        {
            try
            {
                await _context.SaveChangesAsync();

                if (_transaction != null)
                {
                    await _transaction.CommitAsync();
                    _transaction.Dispose();
                    _transaction = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while committing transaction");
                await RollbackTransactionAsync();
                throw;
            }
        }
        
        public async Task RollbackTransactionAsync()
        {
            try
            {
                if (_transaction != null)
                {
                    await _transaction.RollbackAsync();
                    _transaction.Dispose();
                    _transaction = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during transaction rollback");
            }
        }

        // New: Wrap a transaction inside EF execution strategy
        public async Task ExecuteInTransactionAsync(Func<Task> operation)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await operation();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var result = await operation();
                    await transaction.CommitAsync();
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
