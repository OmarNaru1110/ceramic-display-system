using AutoMapper;
using Core.DTOs.ReturnRequest;
using Data.Models;

namespace Core.AutoMapperProfiles
{
    public class ReturnRequestProfile : Profile
    {
        public ReturnRequestProfile()
        {
            CreateMap<ReturnRequest, GetReturnRequestDto>()
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.ReturnItems))
                .ForMember(dest => dest.Order, opt => opt.MapFrom(src => src.Order))
                .ForMember(dest => dest.ReturnPdfUrl, opt => opt.Ignore());

            CreateMap<ReturnItem, GetReturnItemDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name));

            CreateMap<Order, GetOrderInfoDto>();
        }
    }
}
