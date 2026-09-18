using System.Collections.Generic;
using Landfall.AI;
using UnityEngine;

public class PlayerAIDavid : MonoBehaviour
{
	public static int[] topology = new int[4] { 2, 10, 16, 5 };

	[SerializeField]
	private bool m_useWeights;

	[SerializeField]
	private WeightDataAsset m_weightData;

	private PlayerAPI m_api;

	private NeuralNet m_neuralNet;

	private double[] m_cachedInput;

	private float m_startTime;

	private Vector2 m_movement = Vector2.zero;

	private float m_aiTimer;

	private void Awake()
	{
		m_api = GetComponentInParent<PlayerAPI>();
		m_cachedInput = new double[2];
		m_neuralNet = new NeuralNet(topology);
		if (m_weightData != null && m_useWeights)
		{
			m_neuralNet.SetWeights(m_weightData.m_weightDatas[m_weightData.m_weightDatas.Count - 1].m_weights);
		}
		m_startTime = Time.time + 0.8f;
	}

	public void SetWeights(double[] weights)
	{
		m_neuralNet.SetWeights(new List<double>(weights));
	}

	public void SetWeights(List<double> weights)
	{
		m_neuralNet.SetWeights(weights);
	}

	private void Update()
	{
		if (Time.time < m_startTime)
		{
			return;
		}
		if (m_aiTimer >= 0.1f)
		{
			m_aiTimer -= 0.1f;
			Vector3 vector = m_api.OtherPlayerPosition() - m_api.PlayerPosition();
			m_cachedInput[0] = vector.x;
			m_cachedInput[1] = vector.y;
			m_neuralNet.FeedForward(m_cachedInput, 1.0);
			double[] results = m_neuralNet.GetResults();
			m_api.SetAimDirection(new Vector2((float)results[3], (float)results[4]));
			if (results[0] > 0.5)
			{
				m_api.Jump();
			}
			if (results[1] > 0.5)
			{
				m_api.Attack();
			}
			Vector2 zero = Vector2.zero;
			if (results[2] > 0.5)
			{
				zero.x = 1f;
			}
			else if (results[2] < -0.5)
			{
				zero.x = -1f;
			}
			m_movement = zero;
		}
		m_api.Move(m_movement);
		m_aiTimer += TimeHandler.deltaTime;
	}
}
