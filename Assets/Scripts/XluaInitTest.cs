using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XLua;

public class XluaInitTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        LuaEnv luaenv = new LuaEnv();
        luaenv.DoString("CS.UnityEngine.Debug.Log('hello world')");
        luaenv.Dispose();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
