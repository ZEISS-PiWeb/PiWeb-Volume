sampler2D inputSampler : register(S0);

/// <summary>R Factor.</summary>
/// <minValue>0</minValue>
/// <maxValue>1</maxValue>
/// <defaultValue>0.299</defaultValue>
float Low : register(C0);
/// <summary>G Factor.</summary>
/// <minValue>0</minValue>
/// <maxValue>1</maxValue>
/// <defaultValue>0.587</defaultValue>
float High : register(C1);

float4 main(float2 uv : TEXCOORD) : COLOR
{
	float4 color = tex2D(inputSampler, uv);
	if (Low >= High)
		return color;	
	
	float value = color.r;
	float alpha = color.a;

    return lerp(
    float4(0,0,0,alpha), 
    float4(1,1,1,alpha),
    (clamp( value, Low, High) - Low) / (High - Low));   
    
}
