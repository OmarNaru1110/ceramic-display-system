using AutoMapper;
using Core.DTOs.Invoice;
using Data.Models;

namespace Core.AutoMapperProfiles
{
    public class InvoiceProfile : Profile
    {
        public InvoiceProfile()
        {
            CreateMap<Invoice, GetInvoiceDto>()
                .ForMember(dest => dest.Order, opt => opt.MapFrom(src => src.Order));

            CreateMap<Order, GetInvoiceOrderDto>();
        }
    }
}
