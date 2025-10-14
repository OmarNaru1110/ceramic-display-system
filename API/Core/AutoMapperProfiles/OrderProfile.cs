using AutoMapper;
using Core.DTOs.Order;
using Data.Models;

namespace Core.AutoMapperProfiles
{
    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
            CreateMap<Order, GetOrderDto>()
                .ForMember(dest => dest.SellerName, opt => opt.MapFrom(src => src.Seller.UserName))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.OrderItems))
                .ForMember(dest => dest.Invoice, opt => opt.MapFrom(src => src.Invoice));

            CreateMap<OrderItem, GetOrderItemDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name));
        }
    }
}
