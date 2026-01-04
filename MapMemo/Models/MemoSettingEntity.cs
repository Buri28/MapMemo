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

        /// <summary>
        /// ツールチップに BeatSaver の情報を表示するかどうか
        /// </summary>
        /// <remarks>デフォルトは true。</remarks>
        public bool TooltipShowBsr { get; set; } = true;
        /// <summary>
        /// ツールチップにレーティング情報を表示するかどうか
        /// </summary>
        /// <remarks>デフォルトは true。</remarks>
        /// </summary>
        public bool TooltipShowRating { get; set; } = true;
        /// <summary>
        /// 空のメモを自動作成するかどうか
        /// </summary>
        /// <remarks>デフォルトは false。</remarks>
        ///     
        /// </summary>
        public bool AutoCreateEmptyMemo { get; set; } = true;
    }
}