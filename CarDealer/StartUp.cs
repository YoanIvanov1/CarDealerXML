using AutoMapper;
using AutoMapper.QueryableExtensions;
using CarDealer.Data;
using CarDealer.DTOs.Export;
using CarDealer.DTOs.Import;
using CarDealer.Models;
using Castle.Core.Resource;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace CarDealer
{
    public class StartUp
    {
        public static void Main()
        {
            CarDealerContext context = new CarDealerContext();

            string suppliersXml = File.ReadAllText("../../../Datasets/suppliers.xml");

            string partsXml = File.ReadAllText("../../../Datasets/parts.xml");

            string carsXml = File.ReadAllText("../../../Datasets/cars.xml");

            string customersXml = File.ReadAllText("../../../Datasets/customers.xml");

            string salesXml = File.ReadAllText("../../../Datasets/sales.xml");

            Console.WriteLine(GetTotalSalesByCustomer(context));
        }

        private static Mapper GetMapper()
        {
            var config = new MapperConfiguration(c => c.AddProfile<CarDealerProfile>());

            return new Mapper(config);
        }

        //09.
        public static string ImportSuppliers(CarDealerContext context, string inputXml)
        {
            //1.
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ImportSupplierDTO[]),
                new XmlRootAttribute("Suppliers"));
            
            //2.
            using var reader = new StringReader(inputXml);

            ImportSupplierDTO[] importSupplierDTOs =
                DeserializeFromXml<ImportSupplierDTO[]>(inputXml, "Suppliers");



            //3.Map creator
            var mapper = GetMapper();

            Supplier[] suppliers = mapper.Map<Supplier[]>(importSupplierDTOs);

            //4. Add to EF context
            context.AddRange(suppliers);
            context.SaveChanges();

            return $"Successfully imported {suppliers.Count()}";
        }

        //10.
        public static string ImportParts(CarDealerContext context, string inputXml)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ImportPartsDTO[]), 
                new XmlRootAttribute("Parts"));

            using StringReader reader = new StringReader(inputXml);

            ImportPartsDTO[] importPartsDTOs = 
                (ImportPartsDTO[])xmlSerializer.Deserialize(reader);

            var supplierIds = context.Suppliers
                .Select(s => s.Id)
                .ToArray();

            var mapper = GetMapper();

            Part[] parts = mapper.Map<Part[]>(importPartsDTOs
                            .Where(p => supplierIds.Contains(p.SupplierId)));

            context.AddRange(parts);
            context.SaveChanges();

            return $"Successfully imported {parts.Count()}";
        }

        //11.
        public static string ImportCars(CarDealerContext context, string inputXml)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ImportCarsDTO[]),
                new XmlRootAttribute("Cars"));

            using StringReader reader = new StringReader(inputXml);

            ImportCarsDTO[] importCarsDTOs =
                (ImportCarsDTO[])xmlSerializer.Deserialize(reader); 

            var mapper = GetMapper();

            List<Car> cars = new List<Car>();

            foreach (var CarDTO in importCarsDTOs)
            {
                Car car = mapper.Map<Car>(CarDTO);

                int[] carPartsIds = CarDTO.PartsIds
                    .Select(p => p.Id)
                    .Distinct()
                    .ToArray();

                var carParts = new List<PartCar>();

                foreach (var partId in carPartsIds)
                {
                    carParts.Add(new PartCar
                    {
                        Car = car,
                        PartId = partId
                    }); 
                }

                car.PartsCars = carParts;
                cars.Add(car);
            }

            context.AddRange(cars);
            context.SaveChanges();

            return $"Successfully imported {cars.Count()}";
        }

        //12.
        public static string ImportCustomers(CarDealerContext context, string inputXml)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ImportCustomersDTO[]),
                new XmlRootAttribute("Customers"));

            using StringReader reader = new StringReader(inputXml);

            ImportCustomersDTO[] importCustomersDTOs =
                (ImportCustomersDTO[])xmlSerializer.Deserialize(reader);

            var mapper = GetMapper();

            Customer[] customers = mapper.Map<Customer[]>(importCustomersDTOs);

            context.AddRange(customers);
            context.SaveChanges();

            return $"Successfully imported {customers.Count()}";
        }

        //13.
        public static string ImportSales(CarDealerContext context, string inputXml)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ImportSalesDTO[]),
                new XmlRootAttribute("Sales"));

            using StringReader reader = new StringReader(inputXml);

            ImportSalesDTO[] importSalesDTOs =
                (ImportSalesDTO[])xmlSerializer.Deserialize(reader);

            var mapper = GetMapper();

            int[] carIds = context.Cars
                .Select(c => c.Id)
                .ToArray();

            Sale[] sales = mapper.Map<Sale[]>(importSalesDTOs)
                .Where(s => carIds.Contains(s.CarId))
                .ToArray();

            context.AddRange(sales);
            context.SaveChanges();

            return $"Successfully imported {sales.Count()}";
        }

        //14.
        public static string GetCarsWithDistance(CarDealerContext context)
        {
            var mapper = GetMapper();   

            var cars = context.Cars
                .Where(c => c.TraveledDistance > 2000000)
                .OrderBy(c => c.Make)
                    .ThenBy(c => c.Model)
                .Take(10)
                .ProjectTo<ExportCars>(mapper.ConfigurationProvider)
                .ToArray();

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ExportCars[]),
                new XmlRootAttribute("cars"));

            var xsn = new XmlSerializerNamespaces();
            xsn.Add(string.Empty, string.Empty);

            StringBuilder sb = new StringBuilder();

            using(StringWriter sw = new StringWriter(sb))
            {
                xmlSerializer.Serialize(sw, cars, xsn);
            }

            return sb.ToString().Trim();
        }

        //15.
        public static string GetCarsFromMakeBmw(CarDealerContext context)
        {
            var cars = context.Cars
                .Where(c => c.Make == "BMW")
                .Select(c => new ExportCarsBMWsDTO
                {
                    Id = c.Id,
                    Model = c.Model,
                    TraveledDistance = c.TraveledDistance
                })
                .OrderBy(c => c.Model)
                    .ThenByDescending(c => c.TraveledDistance)
                .ToArray();

            return SerializeToXml<ExportCarsBMWsDTO[]>(cars, "cars");
        }

        //16.
        public static string GetLocalSuppliers(CarDealerContext context)
        {
            var suppliers = context.Suppliers
                .Where(s => s.IsImporter == false)
                .Select(s => new ExportLocalSuppliersDTO
                {
                    Id = s.Id,
                    Name = s.Name,
                    PartsCount = s.Parts.Count()
                })
                .ToArray();

            return SerializeToXml<ExportLocalSuppliersDTO[]>(suppliers, "suppliers");
        }

        //17.
        public static string GetCarsWithTheirListOfParts(CarDealerContext context)
        {
            var cars = context.Cars
                .Select(c => new ExportCarsWithPartsDTO
                {
                    Make = c.Make,
                    Model = c.Model,
                    TraveledDistance = c.TraveledDistance,
                    Parts = c.PartsCars
                        .Select(pc => new PartsFromCarsDTO
                        {
                            Name = pc.Part.Name,
                            Price = pc.Part.Price,
                        })
                        .OrderByDescending(pc => pc.Price)
                        .ToList()
                })
                .OrderByDescending(c => c.TraveledDistance)
                    .ThenBy(c => c.Model)
                .Take(5)
                .ToArray();

            return SerializeToXml<ExportCarsWithPartsDTO[]>(cars, "cars");
        }

        //18.
        public static string GetTotalSalesByCustomer(CarDealerContext context)
        {
            var temp = context.Customers
                .Where(c => c.Sales.Any())
                .Select(c => new
                {
                    FullName = c.Name,
                    BoughtCars = c.Sales.Count(),
                    SalesInfo = c.Sales.Select(s => new
                    {
                        Prices = c.IsYoungDriver
                                ? s.Car.PartsCars.Sum(pc => Math.Round((double)pc.Part.Price * 0.95, 2))
                                : s.Car.PartsCars.Sum(pc => (double)pc.Part.Price)
                    })
                        .ToArray(),
                })
                .ToArray();

            ExportSalesByCustomerDTO[] totalSales = temp.OrderByDescending(x => x.SalesInfo.Sum(y => y.Prices))
                .Select(x => new ExportSalesByCustomerDTO()
                {
                    FullName = x.FullName,
                    BoughtCars = x.BoughtCars,
                    SpentMoney = x.SalesInfo.Sum(y => (decimal)y.Prices)
                })
                .ToArray();

            return SerializeToXml<ExportSalesByCustomerDTO[]>(totalSales, "customers");
        }

        //19.
        public static string GetSalesWithAppliedDiscount(CarDealerContext context)
        {
            var sales = context.Sales
                .Select(s => new ExportSalesWithDiscountDTO
                {
                    Car = new CarsWithAppliedDiscountSalesDTO
                    {
                        Make = s.Car.Make,
                        Model = s.Car.Model,
                        TraveledDistance = s.Car.TraveledDistance,
                    },
                    Discount = (int)s.Discount,
                    CustomerName = s.Customer.Name,
                    Price = s.Car.PartsCars.Sum(pc => pc.Part.Price),
                    PriceWithDiscount =
                        Math.Round((double)(s.Car.PartsCars
                            .Sum(p => p.Part.Price) * (1 - (s.Discount / 100))), 4)
                })
                .ToArray();

            return SerializeToXml<ExportSalesWithDiscountDTO[]>(sales, "sales");
        }

        /// <summary>
        /// Generic method to serialize DTOs to XML
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dto"></param>
        /// <param name="xmlRoot"></param>
        /// <returns></returns>
        private static string SerializeToXml<T>(T dto, string xmlRoot)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T),
                new XmlRootAttribute(xmlRoot));

            StringBuilder sb = new StringBuilder();

            using(StringWriter stringWriter = new StringWriter(sb, CultureInfo.InvariantCulture))
            {
                XmlSerializerNamespaces xmlSerializerNamespaces = new XmlSerializerNamespaces();
                xmlSerializerNamespaces.Add(string.Empty, string.Empty);

                try
                {
                    xmlSerializer.Serialize(stringWriter, dto, xmlSerializerNamespaces);
                }
                catch(Exception)
                {
                    throw;
                }
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Generic method to deserialize XML to DTOs
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xml"></param>
        /// <param name="rootElement"></param>
        /// <returns></returns>
        private static T DeserializeFromXml<T>(string xml, string rootElement) where T : class
        {
            T result = default(T);

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T),
                new XmlRootAttribute(rootElement));

            using var reader = new StringReader(xml);

            result = (T)xmlSerializer.Deserialize(reader);

            return result;
        }
    }
}