// Program.cs

using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using oracle_backend.Dbcontexts;
using oracle_backend.Patterns.Factory.Implementations;
using oracle_backend.Patterns.Factory.Interfaces;
using oracle_backend.Patterns.Repository;
using oracle_backend.Patterns.Repository.Implementations;
using oracle_backend.Patterns.Repository.Interfaces;
using oracle_backend.Services;
// [新增] 外观模式的命名空间
using oracle_backend.patterns.Facade_Pattern.Interfaces;
using oracle_backend.patterns.Facade_Pattern.Implementations;


namespace oracle_backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("OracleConnection");
            builder.Services.AddDbContext<AccountDbContext>(options =>
            {
                options.UseOracle(connectionString); // 指定使用 Oracle 提供程序和连接字符串
            });
            // 添加对 ComplexDbContext 的依赖注入
            builder.Services.AddDbContext<ComplexDbContext>(options =>
            {
                options.UseOracle(connectionString);
            });

            // 添加对 EquipmentDbContext 的依赖注入
            builder.Services.AddDbContext<EquipmentDbContext>(options =>
            {
                options.UseOracle(connectionString);
            });

            //添加对 CsahFlowDbcontext的依赖注入
            builder.Services.AddDbContext<CashFlowDbContext>(options =>
            {
                options.UseOracle(connectionString);
            });

            // 添加对 CollaborationDbContext 的依赖注入
            builder.Services.AddDbContext<CollaborationDbContext>(options =>
            {
                options.UseOracle(connectionString);
            });

            builder.Services.AddDbContext<StoreDbContext>(options =>
            {
                options.UseOracle(connectionString); // 添加商店相关的数据库上下文
            });

            builder.Services.AddDbContext<ParkingContext>(options =>
            {
                options.UseOracle(connectionString); // 添加停车场相关的数据库上下文
            });

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // 保持属性名原样，不进行驼峰转换
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                });
          
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // 添加数据库上下文
            builder.Services.AddDbContext<SaleEventDbContext>(options => options.UseOracle(connectionString));

            // 注册服务
            builder.Services.AddScoped<SaleEventService>();

            // 注册仓储模式
            builder.Services.AddScoped<IParkingRepository, ParkingRepository>();
            builder.Services.AddScoped<ISaleEventService, SaleEventService>();
            builder.Services.AddScoped<IAccountRepository, AccountRepository>();
            builder.Services.AddScoped<IStoreRepository, StoreRepository>();
            builder.Services.AddScoped<ISaleEventRepository, SaleEventRepository>();
            builder.Services.AddScoped<IAreaRepository, AreaRepository>();
            builder.Services.AddScoped<ICollaborationRepository, CollaborationRepository>();
            builder.Services.AddScoped<IStoreRepository, StoreRepository>();
            builder.Services.AddScoped<IVenueEventRepository, VenueEventRepository>();
            builder.Services.AddScoped<IEquipmentRepository, EquipmentRepository>();

            //注册工厂模式
            builder.Services.AddScoped<IAreaComponentFactory, AreaComponentFactory>();
            builder.Services.AddScoped<IPersonComponentFactory, PersonComponentFactory>();
            builder.Services.AddScoped<ISaleEventFactory, SaleEventFactory>();
            builder.Services.AddScoped<IAccountFactory, AccountFactory>();
            builder.Services.AddScoped<IVenueEventFactory, VenueEventFactory>();
            builder.Services.AddScoped<IStoreFactory, StoreFactory>();

            //注册外观模式
            builder.Services.AddScoped<IParkingSystemFacade, ParkingSystemFacade>();
            builder.Services.AddScoped<IStoreSystemFacade, StoreSystemFacade>();
            builder.Services.AddScoped<IEquipmentSystemFacade, EquipmentSystemFacade>();
            builder.Services.AddScoped<IVenueEventSystemFacade, VenueEventSystemFacade>();


            // 添加CORS配置
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            //{
            //    app.UseSwagger();
            //    app.UseSwaggerUI();
            //}
            //暂时强制开启 Swagger 等到部署时再关掉
            app.UseSwagger();
            app.UseSwaggerUI();

            //暂时强制关闭 Https 等到部署时再开启
            //app.UseHttpsRedirection();

            // 启用CORS
            app.UseCors("AllowAll");

            app.UseAuthorization();

            app.MapControllers();

            app.Run();

        }
    }
}