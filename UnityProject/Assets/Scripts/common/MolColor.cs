using UnityEngine;
using System.Collections;

public class MolColor {

	public Color rgba; 
	public float L; 
	public float a; 
	public float b; 
	public float X; 
	public float Y; 
	public float Z; 

	// D65 white point 
	static float Xw = 95.047f; 
	static float Yw = 100.000f; 
	static float Zw = 108.883f; 

	static double T1 = 0.008856; 
	static double T2 = 903.3; 
	
	public MolColor (Color c){
		// sRGB
		rgba = c;
		rgba.a = 1.0f; 

		// correct 0-255 ranges to 0-1, if necessary
		if(rgba.r > 1.0f || rgba.g > 1.0f || rgba.b > 1.0f)
		{
			rgba.r /= 255.0f; 
			rgba.g /= 255.0f; 
			rgba.b /= 255.0f; 
		}

		float r_ = correctRGB (rgba.r); 
		float g_ = correctRGB (rgba.g); 
		float b_ = correctRGB (rgba.b); 

		// sRGB --> XYZ
		X = r_ * 0.4124f + g_ * 0.3576f + b_ * 0.1805f; 
		Y = r_ * 0.2126f + g_ * 0.7152f + b_ * 0.0722f; 
		Z = r_ * 0.0193f + g_ * 0.1192f + b_ * 0.9505f; 

		// XYZ --> L*a*b*
		float x_ = correctXYZ (X / Xw); 
		float y_ = correctXYZ (Y / Yw); 
		float z_ = correctXYZ (Z / Zw); 


		L = 116.0f * y_ - 16.0f;
		a = 500.0f * (x_ - y_); 
		b = 200.0f * (y_ - z_); 

		L = Mathf.Max (0.0f, L); 

		//Debug.Log ("Color: [r=" + rgba.r + "; g=" + rgba.g + "; b=" + rgba.b + "] [X=" + X + "; Y=" + Y + "; Z=" + Z + "] [L*=" + L + "; a*=" + a + "; b*=" + b + "]"); 
	}

	public MolColor (float L, float a, float b){
		// L*a*b*
		this.L = L; 
		this.a = a; 
		this.b = b; 

		// L*a*b* --> XYZ
		float y_ = (L + 16.0f) / 116.0f; 
		float x_ = a / 500.0f + y_; 
		float z_ = y_ - b / 200.0f; 

		X = Xw * correctxyz (x_); 
		Y = Yw * correctxyz (y_); //correctL (L); 
		Z = Zw * correctxyz (z_); 

		float X_ = X / 100.0f; 
		float Y_ = Y / 100.0f; 
		float Z_ = Z / 100.0f; 

		rgba.r = X_ * 3.2406f + Y_ * -1.5372f + Z_ * -0.4986f; 
		rgba.g = X_ * -0.9689f + Y_ * 1.8758f + Z_ * 0.0415f; 
		rgba.b = X_ * 0.0557f + Y_ * -0.2040f + Z_ * 1.0570f; 

		rgba.r = correctrgb (rgba.r); 
		rgba.g = correctrgb (rgba.g); 
		rgba.b = correctrgb (rgba.b); 

		rgba.a = 1.0f;

		//Debug.Log ("Color: [r=" + rgba.r + "; g=" + rgba.g + "; b=" + rgba.b + "] [X=" + X + "; Y=" + Y + "; Z=" + Z + "] [L*=" + L + "; a*=" + a + "; b*=" + b + "]"); 
	}

	private float correctRGB(float s){
		if(s > 0.04045){
			s = Mathf.Pow(((s + 0.055f) / 1.055f), 2.4f); 
		}
		else{
			s = s / 12.92f; 
		}
		return (s * 100.0f); 

	}

	private float correctrgb(float s){
		if(s > 0.0031308){
			s = (1.055f * Mathf.Pow (s, 1.0f / 2.4f)) - 0.055f; 
		}
		else{
			s = s * 12.92f; 
		}
		if(s < 0.0f) s = 0.0f;

		return s; 
	}

	private float correctXYZ(float s){
		if (s > T1) {
						return Mathf.Pow (s, 1.0f / 3.0f); 
				}

		return ((7.787f * s) + (16.0f / 116.0f)); 
	}


	private float correctxyz(float s){
		float s3 = Mathf.Pow (s, 3.0f); 
		if (s3 > T1) //{
			return s3; 
			//s = s3; 
		//} else {
		//	s = (s - 16.0f / 116.0f); 
		//}
		//return s / 7.787f; 
			return (s - (16.0f / 116.0f)) / 7.787f; 
	}

	private float correctL(float s){
		if (s > T2 * T1) {
						return Mathf.Pow (((s + 16.0f) / 116.0f), 3.0f); 
				}
		return (float)(s / T2); 

	}
}
