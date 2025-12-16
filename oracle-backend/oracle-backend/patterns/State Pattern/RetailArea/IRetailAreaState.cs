// 商铺区域状态接口
// Refactored with State Pattern

using oracle_backend.Patterns.State.Core;

namespace oracle_backend.Patterns.State.RetailArea
{
    /// <summary>
    /// 商铺区域状态接口
    /// </summary>
    public interface IRetailAreaState : IState<RetailAreaStateContext>
    {
        /// <summary>
        /// 判断是否可以出租
        /// </summary>
        /// <param name="context">区域上下文</param>
        /// <returns>是否可以出租</returns>
        bool CanRent(RetailAreaStateContext context);

        /// <summary>
        /// 租赁区域
        /// </summary>
        /// <param name="context">区域上下文</param>
        /// <param name="tenantInfo">租户信息</param>
        void Rent(RetailAreaStateContext context, string tenantInfo);

        /// <summary>
        /// 释放区域(租约到期或解除)
        /// </summary>
        /// <param name="context">区域上下文</param>
        void Release(RetailAreaStateContext context);
    }
}

