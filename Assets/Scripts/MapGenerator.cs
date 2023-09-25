using UnityEngine;

public class MapGenerator : MonoBehaviour {
    public static MapGenerator instance = null;

    public GameObject map1Prefab;

    private void Awake() {
        if (instance != null && instance != this){
            Destroy(this.gameObject);
            return;
        }

        instance = this;
    }

    public void newMap() {
        ThreadManager.ExecuteOnMainThread(() => {
            MapGenerator.instance.createMap();
        });
    }

    public void createMap() {
        Instantiate(map1Prefab, new Vector3(-0.5236155f,-0.09689212f,-10.69109f), Quaternion.identity);
    }
}