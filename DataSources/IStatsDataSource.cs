using System;

namespace NewUI.DataSources
{
    /// <summary>
    /// 定义统计数据的数据源组件，用于为 UI 提供最新的数据快照。
    /// </summary>
    public interface IStatsDataSource
    {
        /// <summary>
        /// 获取一个包含刷图、收益与交易数据的完整快照。
        /// </summary>
        /// <returns>当前的统计数据快照。</returns>
        StatsSnapshot GetSnapshot();
    }
}
