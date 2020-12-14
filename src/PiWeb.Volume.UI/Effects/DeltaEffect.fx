sampler2D input : register(s0);

sampler2D left : register(s1);

sampler2D right : register(s2);

// new HLSL shader

/// <summary>The minimum color.</summary>
/// <defaultValue>Green</defaultValue>
float4 MinColor : register(C1);

/// <summary>The minimum color.</summary>
/// <defaultValue>Yellow</defaultValue>
float4 MidColor : register(C2);

/// <summary>The minimum color.</summary>
/// <defaultValue>Red</defaultValue>
float4 MaxColor : register(C3);

/// <summary>The minimum value to be marked.</summary>
/// <minValue>0/minValue>
/// <maxValue>1</maxValue>
/// <defaultValue>0.02</defaultValue>
float Min : register(C4);

/// <summary>The maximum value to be marked. Values above will recieve the max color.</summary>
/// <minValue>1/minValue>
/// <maxValue>1</maxValue>
/// <defaultValue>0.05</defaultValue>
float Max : register(C5);

float4 main(float2 uv : TEXCOORD) : COLOR 
{ 
	float4 leftColor = tex2D( left , uv.xy); 
	float4 rightColor = tex2D( right ,uv.xy); 
	
	float value = abs( leftColor.r - rightColor.r);
	
	if ( value < Min || Min >= Max || Max - Min <= 0)
		return float4(0,0,0,0);
	
	if (value >= Max)
		return MaxColor;
		
	float halfRange = 0.5 * (Max - Min);
	float mid = Min + halfRange;
	
	if (value < mid)
		return lerp(MinColor, MidColor, (value - Min) / halfRange);
	else
		return lerp(MidColor, MaxColor, (value - mid) / halfRange);

}