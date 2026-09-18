using UnityEngine.SceneManagement;

public class MapWrapper
{
	public Map Map { get; private set; }

	public Scene Scene { get; private set; }

	public MapWrapper(Map map, Scene scene)
	{
		Map = map;
		Scene = scene;
	}
}
