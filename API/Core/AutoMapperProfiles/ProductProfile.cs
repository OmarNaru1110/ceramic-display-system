using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Core.DTOs.Product;
using Data.Models;

namespace Core.AutoMapperProfiles;
public class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<CreateProductDto, Product>();
        CreateMap<UpdateProductDto, Product>();
        CreateMap<Product, GetProductDto>();
    }
}
