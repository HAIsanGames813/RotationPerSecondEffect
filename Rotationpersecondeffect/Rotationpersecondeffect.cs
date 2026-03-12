using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Effects;

namespace Rotationpersecondeffect
{
    /// <summary>
    /// 指定した秒数で指定した回数回転させる映像エフェクト。
    ///
    /// 1フレームごとに加算する角度 = 回数 × 360 ÷ (秒 × FPS)
    ///
    /// YMM4 は 2×3 行列ベースのため、RotationX / RotationY は
    /// 内部で 4×4 行列に変換して 3D 回転として描画されます。
    /// </summary>
    [VideoEffect("指定秒間回転", ["アニメーション"], [])]
    internal class RotationPerSecondEffect : VideoEffectBase
    {
        // ─── エフェクト名 ────────────────────────────────────────
        public override string Label => "指定秒間回転";

        // ─── パラメーター定義 ─────────────────────────────────────

        /// <summary>何秒間で [回数] 回転するか（周期）</summary>
        [Display(Name = "秒", Description = "何秒間で指定回数回転するか（周期）")]
        [AnimationSlider("F2", "秒", 0.00, 60)]
        public Animation Seconds { get; } = new Animation(1.0, 0.00, 100000000);

        /// <summary>X 軸回転数（小数可、負で逆回転）</summary>
        [Display(Name = "X軸回数", Description = "X軸で何回転するか（小数・負数指定可）")]
        [AnimationSlider("F2", "回", -100, 100)]
        public Animation RotationXCount { get; } = new Animation(0.0, -10000000, 10000000);

        /// <summary>Y 軸回転数（小数可、負で逆回転）</summary>
        [Display(Name = "Y軸回数", Description = "Y軸で何回転するか（小数・負数指定可）")]
        [AnimationSlider("F2", "回", -100, 100)]
        public Animation RotationYCount { get; } = new Animation(0.0, -10000000, 10000000);

        /// <summary>Z 軸回転数（小数可、負で逆回転）</summary>
        [Display(Name = "Z軸回数", Description = "Z軸で何回転するか（小数・負数指定可）")]
        [AnimationSlider("F2", "回", -100, 100)]
        public Animation RotationZCount { get; } = new Animation(0.0, -10000000, 10000000);

        /// <summary>X 軸の初期角度オフセット（度）</summary>
        [Display(Name = "Xオフセット", Description = "X軸の初期角度オフセット（度）")]
        [AnimationSlider("F1", "°", -360, 360)]
        public Animation OffsetX { get; } = new Animation(0.0, -360000000, 360000000);

        /// <summary>Y 軸の初期角度オフセット（度）</summary>
        [Display(Name = "Yオフセット", Description = "Y軸の初期角度オフセット（度）")]
        [AnimationSlider("F1", "°", -360, 360)]
        public Animation OffsetY { get; } = new Animation(0.0, -360000000, 360000000);

        /// <summary>Z 軸の初期角度オフセット（度）</summary>
        [Display(Name = "Zオフセット", Description = "Z軸の初期角度オフセット（度）")]
        [AnimationSlider("F1", "°", -360, 360)]
        public Animation OffsetZ { get; } = new Animation(0.0, -360000000, 360000000);

        // ─── IAnimatable 一覧（アニメーション対応に必要） ──────────
        protected override IEnumerable<IAnimatable> GetAnimatables() =>
        [
            Seconds,
            RotationXCount, RotationYCount, RotationZCount,
            OffsetX, OffsetY, OffsetZ,
        ];

        // ─── EXO 出力（AviUtl 互換なし → 空配列） ─────────────────
        public override IEnumerable<string> CreateExoVideoFilters(
            int keyFrameIndex,
            ExoOutputDescription exoOutputDescription)
            => [];

        // ─── プロセッサー生成 ────────────────────────────────────
        public override IVideoEffectProcessor CreateVideoEffect(
            IGraphicsDevicesAndContext devices)
            => new RotationPerSecondEffectProcessor(this);
    }
}