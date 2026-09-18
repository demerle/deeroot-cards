using System.Collections.Generic;
using UnityEngine;

namespace Landfall.AI
{
	public class NeuralNet
	{
		private Neuron[][] m_layers;

		private double m_bonusWeight = Random.Range(0.2f, 1f);

		public NeuralNet(int[] topology)
		{
			int num = topology.Length;
			m_layers = new Neuron[num][];
			for (int i = 0; i < num; i++)
			{
				bool num2 = i == num - 1;
				int num3 = topology[i];
				if (num2)
				{
					m_layers[i] = new Neuron[num3];
				}
				else
				{
					m_layers[i] = new Neuron[num3 + 1];
				}
				int numOutputs = ((!num2) ? topology[i + 1] : 0);
				if (i < num - 1)
				{
					m_layers[i][num3] = new Neuron(topology[i + 1], num3);
					m_layers[i][num3].OutputValue = 1.0;
				}
				for (int j = 0; j < num3; j++)
				{
					if (i == 0)
					{
						m_layers[i][j] = new Neuron(numOutputs, j);
					}
					else
					{
						m_layers[i][j] = new Neuron(numOutputs, j, m_layers[i - 1]);
					}
				}
			}
		}

		public void FeedForward(double[] inputValues, double bonusInput)
		{
			for (int i = 0; i < inputValues.Length; i++)
			{
				m_layers[0][i].OutputValue = inputValues[i];
			}
			for (int j = 1; j < m_layers.Length; j++)
			{
				int num = ((j == m_layers.Length - 1) ? m_layers[j].Length : (m_layers[j].Length - 1));
				for (int k = 0; k < num; k++)
				{
					if (j == m_layers.Length - 1)
					{
						m_layers[j][k].FeedForward(bonusInput * m_bonusWeight);
					}
					else
					{
						m_layers[j][k].FeedForward();
					}
				}
			}
		}

		public double[] GetResults()
		{
			double[] array = new double[m_layers[m_layers.Length - 1].Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = m_layers[m_layers.Length - 1][i].OutputValue;
			}
			return array;
		}

		public List<double> GetWeights()
		{
			List<double> list = new List<double>();
			for (int i = 0; i < m_layers.Length; i++)
			{
				bool flag = i == m_layers.Length - 1;
				for (int j = 0; j < m_layers[i].Length - ((!flag) ? 1 : 0); j++)
				{
					list.AddRange(m_layers[i][j].GetWeights());
				}
			}
			list.Add(m_bonusWeight);
			return list;
		}

		public void SetWeights(double[] weights)
		{
			SetWeights(new List<double>(weights));
		}

		public void SetWeights(List<double> weights)
		{
			int num = 0;
			for (int i = 0; i < m_layers.Length; i++)
			{
				bool flag = i == m_layers.Length - 1;
				int num2 = m_layers[i].Length - ((!flag) ? 1 : 0);
				for (int j = 0; j < num2; j++)
				{
					num += m_layers[i][j].SetWeights(weights, num);
				}
			}
			m_bonusWeight = weights[num];
		}
	}
}
