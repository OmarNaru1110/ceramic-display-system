using Data.Models;
using DATA.DataAccess.Repositories.IRepositories;
using DATA.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DataAccess.Repositories.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IBaseRepository<AppUser> AppUsers { get; }
        IBaseRepository<Product> Products { get; }
        IBaseRepository<ProductImage> ProductImages { get; }
        IBaseRepository<Cart> Carts { get; }
        IBaseRepository<CartItem> CartItems { get; }
        IBaseRepository<Order> Orders { get; }
        IBaseRepository<OrderItem> OrderItems { get; }
        IBaseRepository<Invoice> Invoices { get; }
        IBaseRepository<ReturnRequest> ReturnRequests { get; }
        IBaseRepository<ReturnItem> ReturnItems { get; }

        Task<int> CommitAsync();

        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();

        // New helpers: execute delegates within EF execution strategy and a transaction
        Task ExecuteInTransactionAsync(Func<Task> operation);
        Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation);
    }
}
