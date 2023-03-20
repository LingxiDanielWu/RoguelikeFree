using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XluaHelloWorld : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        XLua.LuaEnv luaenv = new XLua.LuaEnv();
        luaenv.DoString("print('hello')");
        luaenv.Dispose();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
