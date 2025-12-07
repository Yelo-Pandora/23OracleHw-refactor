using oracle_backend.Models;
using oracle_backend.patterns.Composite_Pattern.Component;
using oracle_backend.patterns.Composite_Pattern.Container;
using oracle_backend.patterns.Composite_Pattern.Leaf;
using oracle_backend.Patterns.Factory.Interfaces;
using oracle_backend.Patterns.Repository.Interfaces;
using static oracle_backend.Controllers.AccountController;
using static oracle_backend.Controllers.StoreController;
using static oracle_backend.Controllers.VenueEventController;

namespace oracle_backend.Patterns.Factory.Implementations
{
    public class AreaComponentFactory : IAreaComponentFactory
    {
        // 工厂持有所有创建 Leaf 所需的 Repository
        private readonly IAreaRepository _areaRepository;
        private readonly IStoreRepository _storeRepository;
        private readonly IParkingRepository _parkingRepository;
        private readonly IVenueEventRepository _venueEventRepository;

        public AreaComponentFactory(
            IAreaRepository areaRepository,
            IStoreRepository storeRepository,
            IParkingRepository parkingRepository,
            IVenueEventRepository venueEventRepository)
        {
            _areaRepository = areaRepository;
            _storeRepository = storeRepository;
            _parkingRepository = parkingRepository;
            _venueEventRepository = venueEventRepository;
        }

        public IAreaComponent Create(int areaId, string category)
        {
            return category.ToUpper() switch
            {
                "RETAIL" => CreateRetail(areaId),
                "PARKING" => CreateParking(areaId),
                "EVENT" => CreateEvent(areaId),
                "OTHER" => CreateOther(areaId),
                _ => throw new ArgumentException($"不支持的区域类型: {category}")
            };
        }

        public IAreaComponent CreateRetail(int areaId)
            => new RetailLeaf(_areaRepository, _storeRepository, areaId);

        public IAreaComponent CreateParking(int areaId)
            => new ParkingLeaf(_areaRepository, _parkingRepository, areaId);

        public IAreaComponent CreateEvent(int areaId)
            => new EventLeaf(_areaRepository, _venueEventRepository, areaId);

        public IAreaComponent CreateOther(int areaId)
            => new OtherLeaf(_areaRepository, areaId);
    }

    public class PersonComponentFactory : IPersonComponentFactory
    {
        private readonly ICollaborationRepository _collabRepo;
        private readonly IAccountRepository _accountRepo;
        private readonly IVenueEventRepository _eventRepo;

        public PersonComponentFactory(
            ICollaborationRepository collabRepo,
            IAccountRepository accountRepo,
            IVenueEventRepository eventRepo)
        {
            _collabRepo = collabRepo;
            _accountRepo = accountRepo;
            _eventRepo = eventRepo;
        }

        public IPersonComponent CreateStaff(int staffId)
        {
            return new StaffLeaf(_collabRepo, _accountRepo, _eventRepo, staffId);
        }

        public IPersonComponent CreateDepartment(string departmentName)
        {
            return new DepartmentComposite(departmentName);
        }
    }

    public class SaleEventFactory : ISaleEventFactory
    {
        public SaleEvent CreateSaleEvent(int newId, SaleEventDto dto)
        {
            return new SaleEvent
            {
                EVENT_ID = newId,
                EVENT_NAME = dto.EventName,
                Cost = dto.Cost,
                EVENT_START = dto.EventStart,
                EVENT_END = dto.EventEnd,
                Description = dto.Description,
                // 这里可以统一处理默认值，例如创建时间等
            };
        }

        public PartStore CreatePartStore(int eventId, int storeId)
        {
            return new PartStore
            {
                EVENT_ID = eventId,
                STORE_ID = storeId
            };
        }

        public SaleEventReport CreateReport(SaleEvent saleEvent, double salesIncrement, double couponRedemptionRate)
        {
            // 将 ROI 计算逻辑封装在工厂内部，Service 不需要关心数学公式
            double roi = saleEvent.Cost == 0 ? 0 : (salesIncrement - saleEvent.Cost) / saleEvent.Cost;

            return new SaleEventReport
            {
                EventId = saleEvent.EVENT_ID,
                EventName = saleEvent.EVENT_NAME,
                SalesIncrement = salesIncrement,
                Cost = saleEvent.Cost,
                ROI = roi,
                CouponRedemptionRate = couponRedemptionRate
            };
        }
    }

    public class AccountFactory : IAccountFactory
    {
        public Account CreateAccount(AccountRegisterDto dto, bool isFirstUser)
        {
            int authorityCode;
            // 封装权限判断逻辑
            if (isFirstUser)
            {
                authorityCode = 1; // 管理员
            }
            else
            {
                authorityCode = dto.IDENTITY switch
                {
                    "员工" => 3,
                    "商户" => 4,
                    _ => throw new ArgumentException($"无效的身份类型：'{dto.IDENTITY}'")
                };
            }

            return new Account
            {
                ACCOUNT = dto.ACCOUNT,
                USERNAME = dto.USERNAME,
                PASSWORD = dto.PASSWORD,
                IDENTITY = isFirstUser ? "员工" : dto.IDENTITY, // 第一个用户强制设为员工(管理员)身份
                AUTHORITY = authorityCode
            };
        }

        public StaffAccount CreateStaffLink(string account, int staffId)
        {
            return new StaffAccount { ACCOUNT = account, STAFF_ID = staffId };
        }

        public StoreAccount CreateStoreLink(string account, int storeId)
        {
            return new StoreAccount { ACCOUNT = account, STORE_ID = storeId };
        }
    }

    public class VenueEventFactory : IVenueEventFactory
    {
        public (VenueEvent Event, VenueEventDetail Detail) CreateReservation(VenueEventReservationDto dto, int defaultCapacity)
        {
            var venueEvent = new VenueEvent
            {
                EVENT_NAME = dto.EventName,
                EVENT_START = dto.RentStartTime,
                EVENT_END = dto.RentEndTime,
                HEADCOUNT = dto.ExpectedHeadcount,
                FEE = dto.ExpectedFee ?? 0,
                CAPACITY = dto.Capacity ?? defaultCapacity,
                EXPENSE = dto.Expense ?? 0
            };

            var detail = new VenueEventDetail
            {
                // EVENT_ID 将在主表保存后获取
                AREA_ID = dto.AreaId,
                COLLABORATION_ID = dto.CollaborationId,
                RENT_START = dto.RentStartTime,
                RENT_END = dto.RentEndTime,
                STATUS = "待审批", // 默认状态封装在此
                FUNDING = 0
            };

            return (venueEvent, detail);
        }

        public TempAuthority CreateTempAuthority(string account, int eventId, int authorityLevel)
        {
            return new TempAuthority
            {
                ACCOUNT = account,
                EVENT_ID = eventId,
                TEMP_AUTHORITY = authorityLevel
            };
        }
    }

    public class StoreFactory : IStoreFactory
    {
        public RetailArea CreateRetailArea(CreateRetailAreaDto dto)
        {
            return new RetailArea
            {
                AREA_ID = dto.AreaId,
                ISEMPTY = 1,
                AREA_SIZE = dto.AreaSize,
                CATEGORY = "RETAIL",
                RENT_STATUS = "空置",
                BASE_RENT = dto.BaseRent
            };
        }

        public MerchantAggregate CreateMerchantAggregate(CreateMerchantDto dto, int newStoreId)
        {
            // 1. 生成商户账号名
            var accountName = $"store_{newStoreId:D6}";

            // 2. 生成随机密码
            var password = GenerateRandomPassword(8);

            // 3. 创建各个实体
            var store = new Store
            {
                STORE_ID = newStoreId,
                STORE_NAME = dto.StoreName,
                STORE_STATUS = "正常营业",
                STORE_TYPE = dto.StoreType,
                TENANT_NAME = dto.TenantName,
                CONTACT_INFO = dto.ContactInfo,
                RENT_START = dto.RentStart,
                RENT_END = dto.RentEnd
            };

            var rentStore = new RentStore
            {
                STORE_ID = newStoreId,
                AREA_ID = dto.AreaId
            };

            var account = new Account
            {
                ACCOUNT = accountName,
                PASSWORD = password,
                USERNAME = dto.TenantName,
                IDENTITY = "商户",
                AUTHORITY = 4
            };

            var storeAccount = new StoreAccount
            {
                ACCOUNT = accountName,
                STORE_ID = newStoreId
            };

            return new MerchantAggregate
            {
                Store = store,
                RentStore = rentStore,
                Account = account,
                StoreAccount = storeAccount,
                GeneratedPassword = password
            };
        }

        public (Store Store, RentStore RentStore, StoreAccount Link) CreateMerchantWithExistingAccount(CreateMerchantDto dto, int newStoreId)
        {
            var store = new Store
            {
                STORE_ID = newStoreId,
                STORE_NAME = dto.StoreName,
                STORE_STATUS = "正常营业",
                STORE_TYPE = dto.StoreType,
                TENANT_NAME = dto.TenantName,
                CONTACT_INFO = dto.ContactInfo,
                RENT_START = dto.RentStart,
                RENT_END = dto.RentEnd
            };

            var rentStore = new RentStore { STORE_ID = newStoreId, AREA_ID = dto.AreaId };
            var link = new StoreAccount { ACCOUNT = dto.OperatorAccount, STORE_ID = newStoreId };

            return (store, rentStore, link);
        }

        // 私有辅助方法
        private string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}