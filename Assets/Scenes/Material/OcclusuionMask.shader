Shader "Unlit/OcclusionMask"
{
    SubShader
    {
        // 다른 불투명 오브젝트들보다 먼저 그려지도록 설정합니다.
        Tags { "Queue" = "Geometry-1" }

        Pass
        {
            // 중요: 색상 정보를 전혀 그리지 않아 투명하게 만듭니다.
            ColorMask 0

            // 중요: 깊이 정보는 기록하여 뒤에 있는 오브젝트를 가립니다.
            ZWrite On
        }
    }
}