using UnityEngine;

public class AudioManager : MonoBehaviour{
    // Varijabla koja će čuvati jedinu instancu AudioManagera
    public static AudioManager instance;

    void Awake(){
        if (instance == null){
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else{
            Destroy(gameObject);
        }
    }
}