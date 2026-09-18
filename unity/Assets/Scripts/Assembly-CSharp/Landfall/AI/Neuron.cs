using System;
using System.Collections.Generic;
using UnityEngine;

namespace Landfall.AI
{
	public class Neuron
	{
		private class Connection
		{
			public double m_weight;

			public double m_deltaWeight;
		}

		private static double _ETA = 0.15;

		private static double _ALPHA = 0.5;

		private double m_outputValue;

		private double m_gradient;

		private int m_index;

		private Dictionary<int, Connection> m_outputWeights = new Dictionary<int, Connection>();

		private Neuron[] m_prevLayer;

		public double OutputValue
		{
			get
			{
				return m_outputValue;
			}
			set
			{
				m_outputValue = value;
			}
		}

		public Neuron(int numOutputs, int index, Neuron[] prevLayer)
			: this(numOutputs, index)
		{
			m_prevLayer = prevLayer;
		}

		public Neuron(int numOutputs, int index)
		{
			m_index = index;
			for (int i = 0; i < numOutputs; i++)
			{
				Connection value = new Connection
				{
					m_weight = UnityEngine.Random.Range(-0.5f, 0.5f)
				};
				m_outputWeights.Add(i, value);
			}
		}

		public void FeedForward()
		{
			double prevLayerSum = GetPrevLayerSum();
			m_outputValue = TransferFunction(prevLayerSum);
		}

		public void FeedForward(double bonusInput)
		{
			double prevLayerSum = GetPrevLayerSum();
			prevLayerSum += bonusInput;
			m_outputValue = TransferFunction(prevLayerSum);
		}

		private double GetPrevLayerSum()
		{
			double num = 0.0;
			if (m_prevLayer != null)
			{
				for (int i = 0; i < m_prevLayer.Length; i++)
				{
					Neuron neuron = m_prevLayer[i];
					num += neuron.OutputValue * neuron.m_outputWeights[m_index].m_weight;
				}
			}
			return num;
		}

		public List<double> GetWeights()
		{
			List<double> list = new List<double>();
			List<int> list2 = new List<int>(m_outputWeights.Keys);
			list2.Sort();
			for (int i = 0; i < list2.Count; i++)
			{
				list.Add(m_outputWeights[list2[i]].m_weight);
			}
			return list;
		}

		public int SetWeights(List<double> weights, int offset)
		{
			List<int> list = new List<int>(m_outputWeights.Keys);
			list.Sort();
			for (int i = 0; i < list.Count; i++)
			{
				m_outputWeights[list[i]].m_weight = weights[offset + i];
			}
			return list.Count;
		}

		private static double TransferFunction(double x)
		{
			return Math.Tanh(x);
		}

		private static double TransferFunctionDerivative(double x)
		{
			return 1.0 - x * x;
		}
	}
}
