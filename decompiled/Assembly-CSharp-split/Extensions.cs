using System;

public static class Extensions
{
	public static double NextGaussianDouble(this Random r)
	{
		double num;
		double num3;
		do
		{
			num = 2.0 * r.NextDouble() - 1.0;
			double num2 = 2.0 * r.NextDouble() - 1.0;
			num3 = num * num + num2 * num2;
		}
		while (num3 >= 1.0);
		double num4 = Math.Sqrt(-2.0 * Math.Log(num3) / num3);
		return num * num4;
	}
}
