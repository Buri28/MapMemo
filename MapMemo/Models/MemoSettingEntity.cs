namespace Mapmemo.Models
{
    public class MemoSettingEntity
    {
        /// <summary>
        /// 履歴の最大保存件数
        /// </summary>
        public int HistoryMaxCount { get; set; } = 1000;
        /// <summary>
        /// 履歴の表示件数
        /// </summary>
        public int HistoryShowCount { get; set; } = 3;
    }
}