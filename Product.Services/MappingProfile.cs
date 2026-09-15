using AutoMapper;
using Product.Data;

namespace Product.Services;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<CreateProductRequest, Product.Data.Product>();
        CreateMap<UpdateProductRequest, Product.Data.Product>();
        CreateMap<Product.Data.Product, ProductResponse>();
    }
}