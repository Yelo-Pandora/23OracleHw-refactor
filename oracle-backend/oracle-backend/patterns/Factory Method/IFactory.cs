using oracle_backend.Models;
using oracle_backend.patterns.Composite_Pattern.Component;
using System.Threading.Tasks;
using static oracle_backend.Controllers.AccountController;
using static oracle_backend.Controllers.StoreController;
using static oracle_backend.Controllers.VenueEventController;

namespace oracle_backend.Patterns.Factory.Interfaces
{
    // 负责创建区域相关的组件 (Retail, Event, Parking, Other)
    public interface IAreaComponentFactory
    {
        // 根据类型字符串创建组件
        IAreaComponent Create(int areaId, string category);

        // 为了方便特定控制器，也可以提供强类型的创建方法
        IAreaComponent CreateRetail(int areaId);
        IAreaComponent CreateParking(int areaId);
        IAreaComponent CreateEvent(int areaId);
        IAreaComponent CreateOther(int areaId);
    }

    // 负责创建人员相关的组件 (Staff, Department)
    public interface IPersonComponentFactory
    {
        IPersonComponent CreateStaff(int staffId);
        IPersonComponent CreateDepartment(string departmentName);
    }

    public interface ISaleEventFactory
    {
        // 负责将 DTO 转换为实体
        SaleEvent CreateSaleEvent(int newId, SaleEventDto dto);

        // 负责创建多对多关联实体
        PartStore CreatePartStore(int eventId, int storeId);

        // 负责生成报表，封装 ROI 计算逻辑
        SaleEventReport CreateReport(SaleEvent saleEvent, double salesIncrement, double couponRedemptionRate);
    }

    public interface IAccountFactory
    {
        // 创建主账号 (处理权限逻辑)
        Account CreateAccount(AccountRegisterDto dto, bool isFirstUser);

        // 创建员工关联
        StaffAccount CreateStaffLink(string account, int staffId);

        // 创建商户关联
        StoreAccount CreateStoreLink(string account, int storeId);
    }

    public interface IVenueEventFactory
    {
        // 创建预约聚合 (包含 Event 和 Detail)
        (VenueEvent Event, VenueEventDetail Detail) CreateReservation(VenueEventReservationDto dto, int defaultCapacity);

        // 创建临时权限
        TempAuthority CreateTempAuthority(string account, int eventId, int authorityLevel);
    }

    // 定义一个聚合对象来承载商户创建的所有产物
    public class MerchantAggregate
    {
        public Store Store { get; set; }
        public RentStore RentStore { get; set; }
        public Account Account { get; set; }
        public StoreAccount StoreAccount { get; set; }
        public string GeneratedPassword { get; set; } // 需要返回给前端展示
    }

    public interface IStoreFactory
    {
        // 创建零售区域
        RetailArea CreateRetailArea(CreateRetailAreaDto dto);

        // 创建商户聚合 (核心重构点)
        MerchantAggregate CreateMerchantAggregate(CreateMerchantDto dto, int newStoreId);

        // 基于现有账号创建商户
        (Store Store, RentStore RentStore, StoreAccount Link) CreateMerchantWithExistingAccount(CreateMerchantDto dto, int newStoreId);
    }
}