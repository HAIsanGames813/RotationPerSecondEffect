using Vortice.Direct2D1;
using YukkuriMovieMaker.Player.Video;

namespace Rotationpersecondeffect
{
    /// <summary>
    /// 毎フレーム呼び出される処理クラス。
    ///
    /// ■ 方針
    ///   角度(N) = offset(frame=0) + Σ delta(i)  (i=1 to N)
    ///   delta(i) = rotCount(i) × 360 ÷ (seconds(i) × fps)
    ///
    ///   これにより:
    ///   - イージングで rotCount が変化しても各フレームの delta が変わるだけで正確に蓄積される
    ///   - フレーム番号だけで一意に角度が決まるため、停止・再開しても値が変わらない
    ///   - 前向き再生はキャッシュで O(1)、巻き戻し時のみ frame=0 から再合計する
    /// </summary>
    internal class RotationPerSecondEffectProcessor : IVideoEffectProcessor
    {
        readonly RotationPerSecondEffect effect;

        ID2D1Image? input;

        // ─── キャッシュ ─────────────────────────────────────────
        int cachedFrame = -1;   // 直近に計算済みのフレーム番号（-1=未初期化）
        double accumX, accumY, accumZ;

        public ID2D1Image Output =>
            input ?? throw new System.InvalidOperationException("Input が設定されていません。");

        public RotationPerSecondEffectProcessor(RotationPerSecondEffect effect)
        {
            this.effect = effect;
        }

        public void SetInput(ID2D1Image? input) => this.input = input;
        public void ClearInput() => input = null;

        public DrawDescription Update(EffectDescription effectDescription)
        {
            var frame = effectDescription.ItemPosition.Frame;  // アイテム先頭からの相対フレーム
            var length = effectDescription.ItemDuration.Frame;
            var fps = effectDescription.FPS;

            // ─── キャッシュの有効範囲を決める ───────────────────────
            int startFrame;
            if (cachedFrame < 0 || frame < cachedFrame)
            {
                // 未初期化 or 巻き戻し → frame=0 のオフセットから再計算
                accumX = effect.OffsetX.GetValue(0, length, fps);
                accumY = effect.OffsetY.GetValue(0, length, fps);
                accumZ = effect.OffsetZ.GetValue(0, length, fps);
                startFrame = 1;   // delta は frame=1 から加算
            }
            else
            {
                // 前回の続きから加算（前向き再生の通常ケース）
                startFrame = cachedFrame + 1;
            }

            // ─── startFrame から現在フレームまで delta を合計 ───────
            for (int i = startFrame; i <= frame; i++)
            {
                var seconds = effect.Seconds.GetValue(i, length, fps);
                var cycleFrames = seconds * fps;

                if (cycleFrames > 0.0)
                {
                    accumX += effect.RotationXCount.GetValue(i, length, fps) * 360.0 / cycleFrames;
                    accumY += effect.RotationYCount.GetValue(i, length, fps) * 360.0 / cycleFrames;
                    accumZ += effect.RotationZCount.GetValue(i, length, fps) * 360.0 / cycleFrames;
                }
            }

            cachedFrame = frame;

            // ─── DrawDescription の Rotation を更新して返す ─────────
            var desc = effectDescription.DrawDescription;
            var oldRot = desc.Rotation;

            return desc with
            {
                Rotation = oldRot with
                {
                    X = (float)(oldRot.X + accumX),
                    Y = (float)(oldRot.Y + accumY),
                    Z = (float)(oldRot.Z + accumZ),
                }
            };
        }

        public void Dispose()
        {
            input = null;
        }
    }
}