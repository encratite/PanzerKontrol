using System;

class NormalDistribution
{
	double Mean;
	double Deviation;
	double[] Results;
	int Index;

	Random Generator;

	public NormalDistribution(double mean, double deviation)
	{
		Mean = mean;
		Deviation = deviation;
		Results = new double[2];
		Index = Results.Length;
		Generator = new Random();
	}

	public double Get()
	{
		if (Index == Results.Length)
		{
			Index = 0;
			BoxMullerTransform();
		}
		double output = Results[Index];
		Index++;
		return output;
	}

	double Adjust(double x)
	{
		return Mean + Deviation * x;
	}

	void BoxMullerTransform()
	{
		double u1 = Generator.NextDouble();
		double u2 = Generator.NextDouble();
		double theta = 2 * Math.PI * u2;
		double r = Math.Sqrt(-2 * Math.Log(u1));
		double z0 = r * Math.Cos(theta);
		double z1 = r * Math.Sin(theta);
		Results[0] = Adjust(z0);
		Results[1] = Adjust(z1);
	}
}
