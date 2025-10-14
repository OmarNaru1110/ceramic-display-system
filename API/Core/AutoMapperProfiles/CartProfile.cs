using AutoMapper;
using Core.DTOs.Cart;
using Data.Models;

namespace Core.AutoMapperProfiles
{
    public class CartProfile : Profile
    {
        public CartProfile()
        {
            CreateMap<Cart, GetCartDto>()
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.CartItems))
                .ForMember(dest => dest.TotalAmount, opt => opt.Ignore());

            CreateMap<CartItem, GetCartItemDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name))
                .ForMember(dest => dest.PricePerSqm, opt => opt.MapFrom(src => src.Product.PricePerSqm))
                .ForMember(dest => dest.ItemTotal, opt => opt.Ignore());
        }
    }
}
