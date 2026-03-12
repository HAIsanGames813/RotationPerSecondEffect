using Vortice.Direct2D1;
using YukkuriMovieMaker.Player.Video;

namespace Rotationpersecondeffect
{
    /// <summary>
    /// 毎フレーム呼び出される処理クラス。
    /// 画像処理は行わず（パススルー）。
    ///
    /// 蓄積方式：1フレームごとに
    ///   deltaAngle = count × 360 ÷ (seconds × fps)
    /// を前フレームの角度に加算していく。
    /// タイムラインを巻き戻した場合はリセットして再計算する。
    /// </summary>
    internal class RotationPerSecondEffectProcessor : IVideoEffectProcessor
    {
        readonly RotationPerSecondEffect effect;

        // ─── 蓄積角度（フレーム0でのオフセット込み） ──────────────
        double accumX, accumY, accumZ;

        // 直前に処理したフレーム番号（-1 = 未初期化）
        int prevFrame = -1;

        // 入力画像をそのまま出力する（パススルー）
        ID2D1Image? input;

        public ID2D1Image Output =>
            input ?? throw new System.InvalidOperationException("Input が設定されていません。");

        public RotationPerSecondEffectProcessor(RotationPerSecondEffect effect)
        {
            this.effect = effect;
        }

        public void SetInput(ID2D1Image? input) => this.input = input;
        public void ClearInput() => input = null;

        /// <summary>
        /// フレームごとに呼ばれる更新処理。
        /// 前フレームからの差分角度を蓄積して返す。
        /// </summary>
        public DrawDescription Update(EffectDescription effectDescription)
        {
            var frame = effectDescription.ItemPosition.Frame;
            var length = effectDescription.ItemDuration.Frame;
            var fps = effectDescription.FPS;

            // ─── リセット判定 ─────────────────────────────────────
            // frame が 0 に戻った、または前回より小さい（巻き戻し）場合は
            // フレーム0からオフセット値で再スタートする。
            if (frame == 0 || frame < prevFrame)
            {
                accumX = effect.OffsetX.GetValue(0, length, fps);
                accumY = effect.OffsetY.GetValue(0, length, fps);
                accumZ = effect.OffsetZ.GetValue(0, length, fps);
                prevFrame = -1;
            }

            // ─── フレーム0 の初回処理 ─────────────────────────────
            if (prevFrame < 0)
            {
                // オフセットのみ適用。delta は加算しない（frame 0 は静止開始）
                prevFrame = frame;
            }
            else
            {
                // ─── 前フレームから今フレームまでの差分フレーム数ぶん加算 ──
                // （通常は 1F ずつだが、スキップされた場合も正しく動作する）
                var frameDelta = frame - prevFrame;

                var seconds = effect.Seconds.GetValue(frame, length, fps);
                var cycleFrames = seconds * fps;

                if (cycleFrames > 0.0)
                {
                    // 1F あたりの角度 = count × 360 ÷ cycleFrames
                    var perFrameX = effect.RotationXCount.GetValue(frame, length, fps) * 360.0 / cycleFrames;
                    var perFrameY = effect.RotationYCount.GetValue(frame, length, fps) * 360.0 / cycleFrames;
                    var perFrameZ = effect.RotationZCount.GetValue(frame, length, fps) * 360.0 / cycleFrames;

                    accumX += perFrameX * frameDelta;
                    accumY += perFrameY * frameDelta;
                    accumZ += perFrameZ * frameDelta;
                }
                // cycleFrames <= 0 のときは角度を加算しない（停止扱い）

                prevFrame = frame;
            }

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