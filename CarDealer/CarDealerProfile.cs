using AutoMapper;
using CarDealer.DTOs.Export;
using CarDealer.DTOs.Import;
using CarDealer.Models;

namespace CarDealer
{
    public class CarDealerProfile : Profile
    {
        public CarDealerProfile()
        {
            CreateMap<ImportSupplierDTO, Supplier>();
            CreateMap<ImportPartsDTO, Part>();
            CreateMap<ImportCarsDTO, Car>();
            CreateMap<ImportCustomersDTO, Customer>();
            CreateMap<ImportSalesDTO, Sale>();

            CreateMap<Car, ExportCars>();
            CreateMap<Customer, ExportSalesByCustomerDTO>();
            CreateMap<Car, ExportCarsBMWsDTO>();
            CreateMap<Supplier, ExportLocalSuppliersDTO>();
            CreateMap<Car, ExportCarsWithPartsDTO>();
            CreateMap<PartCar, PartsFromCarsDTO>();
            CreateMap<Car, CarsWithAppliedDiscountSalesDTO>();

            CreateMap<Car, ImportCarsDTO>();
        }
    }
}
