namespace AntShares.Core
{
    /// <summary>
    /// 投票信息
    /// </summary>
    public class Vote
    {
        /// <summary>
        /// 报名表的散列值列表
        /// </summary>
        public UInt256[] Enrollments;
        /// <summary>
        /// 选票的数目
        /// </summary>
        public Fixed8 Count;
    }
}
