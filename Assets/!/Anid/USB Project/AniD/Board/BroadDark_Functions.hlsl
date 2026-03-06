// ================================================================
//  BroadDark_Functions.hlsl
//  Custom functions cho Unity 6 Shader Graph
//  Tương đương node graph Blender: Texture → ColorRamp → Multiply → BSDF
// ================================================================

// ── 1. COLOR RAMP (Blender Linear mode) ─────────────────────────
// Inputs:
//   T          : Float (Factor từ texture color)
//   Stop0Color : Vector3 (màu stop 0, default black)
//   Stop0Pos   : Float   (position stop 0, default 0)
//   Stop1Color : Vector3 (màu stop 1, default white)
//   Stop1Pos   : Float   (position stop 1, default 1)
// Output:
//   Out        : Vector3 (màu kết quả)
void ColorRamp_Linear_float(
    float  T,
    float3 Stop0Color, float Stop0Pos,
    float3 Stop1Color, float Stop1Pos,
    out float3 Out)
{
    float t = saturate(T);
    float span = Stop1Pos - Stop0Pos;
    float local = (span > 0.0001) ? saturate((t - Stop0Pos) / span) : 0.0;
    // Ease interpolation giống Blender default
    float ft = local * local * (3.0 - 2.0 * local);
    Out = lerp(Stop0Color, Stop1Color, ft);
}

// Overload với 4 stops (mở rộng nếu cần)
void ColorRamp4_float(
    float  T,
    float3 C0, float P0,
    float3 C1, float P1,
    float3 C2, float P2,
    float3 C3, float P3,
    out float3 Out)
{
    float t = saturate(T);
    float3 stops[4]  = { C0, C1, C2, C3 };
    float  poses[4]  = { P0, P1, P2, P3 };

    Out = C0;
    for (int i = 0; i < 3; i++)
    {
        if (t >= poses[i] && t <= poses[i + 1])
        {
            float span  = poses[i + 1] - poses[i];
            float local = (span > 0.0001) ? (t - poses[i]) / span : 0.0;
            float ft    = local * local * (3.0 - 2.0 * local); // ease
            Out = lerp(stops[i], stops[i + 1], ft);
            return;
        }
    }
    Out = C3;
}

// ── 2. BLEND: MULTIPLY (Blender Multiply mode) ──────────────────
// Blend = A * B,  Factor điều chỉnh blend với A gốc
// Inputs:
//   ColorA  : Vector3
//   ColorB  : Vector3
//   Factor  : Float (0-1, default ~0.956 như trong ảnh)
// Output:
//   Out     : Vector3
void BlendMultiply_float(
    float3 ColorA,
    float3 ColorB,
    float  Factor,
    out float3 Out)
{
    float3 blended = ColorA * ColorB;
    Out = lerp(ColorA, blended, saturate(Factor));
}

// ── 3. BLEND: LIGHTEN (Blender Lighten mode) ────────────────────
// Blend = max(A, B), Factor = 1.000
// Inputs:
//   ColorA  : Vector3
//   B       : Float (Alpha của texture trong ảnh)
//   Factor  : Float (default 1.0)
// Output:
//   Out     : Float (hohoặc Vector3 nếu A là Vector3)
void BlendLighten_float(
    float3 ColorA,
    float3 B,
    float  Factor,
    out float3 Out)
{
    float3 blended = max(ColorA, B); // per-channel lighten
    Out = lerp(ColorA, blended, saturate(Factor));
}

// Overload: cả hai input là float
void BlendLightenFloat_float(
    float A,
    float B,
    float Factor,
    out float Out)
{
    float blended = max(A, B);
    Out = lerp(A, blended, saturate(Factor));
}

// ── 4. FULL PIPELINE (tất cả trong 1 node) ──────────────────────
// Tương đương toàn bộ node graph:
//   Texture → ColorRamp → Multiply → Base Color
//   Texture Alpha → Lighten → Alpha
// Inputs:
//   TexColor     : Vector3 (RGB từ texture sample)
//   TexAlpha     : Float   (A từ texture sample)
//   RampStop0    : Vector3 (màu stop 0 của Color Ramp)
//   RampStop1    : Vector3 (màu stop 1 của Color Ramp)
//   MultiplyFac  : Float   (Multiply blend factor, ~0.956)
//   LightenFac   : Float   (Lighten blend factor, 1.0)
// Outputs:
//   BaseColor    : Vector3
//   Alpha        : Float
void BroadDarkPipeline_float(
    float3 TexColor,
    float  TexAlpha,
    float3 RampStop0,
    float3 RampStop1,
    float  MultiplyFac,
    float  LightenFac,
    out float3 BaseColor,
    out float  Alpha)
{
    // Step 1: Color Ramp (Factor = texture luminance)
    float factor = dot(TexColor, float3(0.299, 0.587, 0.114));
    float ft = factor * factor * (3.0 - 2.0 * factor); // ease
    float3 rampColor = lerp(RampStop0, RampStop1, ft);

    // Step 2: Multiply blend (TexColor * RampColor)
    float3 multiplied = TexColor * rampColor;
    BaseColor = lerp(TexColor, multiplied, saturate(MultiplyFac));

    // Step 3: Lighten (max of Multiply result lum vs TexAlpha)
    float baseLum = dot(BaseColor, float3(0.299, 0.587, 0.114));
    float lightened = max(baseLum, TexAlpha);
    Alpha = lerp(baseLum, lightened, saturate(LightenFac));
}
