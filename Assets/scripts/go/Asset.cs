using UnityEngine;
using KBEngine;
using System.Collections;
using System;
using System.Xml;
using System.Collections.Generic;

public class AssetAsyncLoadObjectCB
{
	public virtual void onAssetAsyncLoadObjectCB(string name, UnityEngine.Object obj, Asset asset)
	{
	}
}

public class Asset 
{
	public AssetLoad load = null;
	public string createAtScene = "";
	
	public enum TYPE
	{
		NORMAL = 0,
		SKYBOX = 1,
		TERRAIN = 2,
		TERRAIN_DETAIL_TEXTURE = 3,
		TERRAIN_TREE = 4,
		WORLD_OBJ = 5
	}
	
	public enum LOAD_LEVEL
	{
		LEVEL_IDLE = 0,
		LEVEL_ENTER_BEFORE = 1,
		LEVEL_ENTER_AFTER = 2,
		LEVEL_SCRIPT_DYNAMIC = 3,
		LEVEL_SPAWNPOINT = 4
	}

	public enum UNLOAD_LEVEL
	{
		LEVEL_NORMAL = 0,
		LEVEL_FORBID = 1,
		LEVEL_FORBID_GAMEOBJECT = 2
	}
	
	public List<string> refs = new List<string>();
	
	public TYPE type = TYPE.NORMAL;
	
	public UInt16 loadPri = 99;
	public LOAD_LEVEL loadLevel = LOAD_LEVEL.LEVEL_IDLE;
	public UNLOAD_LEVEL unloadLevel = UNLOAD_LEVEL.LEVEL_NORMAL;
	
    public Asset() {  
		isLoaded = false;
    }  
  	
  	public void setLoadPri(UInt16 pri)
  	{
  		loadPri = pri;
  		loader.inst.loadPool.sortPri();
  	}
  	
    public string source {  
        get;  
        set;  
    }  
	
    public bool isLoaded {  
        get;  
        set;  
    }  
  
    public bool loading {  
        get;  
        set;  
    }  

    public string layerName {  
        get;  
        set;  
    }  
    
    public AssetBundle bundle {  
        get;  
        set;  
    } 
	
    public UnityEngine.Object mainAsset {  
        get;  
        set;  
    } 
	
	public IEnumerator _InstantiateObj(AssetAsyncLoadObjectCB cb, string name, Vector3 position, Vector3 rotation, Vector3 scale, bool setposdir)
	{
		AssetBundleRequest request = null;
		bool isTypeGameObject = false;
		
		System.Type otype = null;
		switch(type)
		{
		case Asset.TYPE.TERRAIN_DETAIL_TEXTURE:
			otype = typeof(UnityEngine.Texture2D);
			break;
		default:
			otype = typeof(UnityEngine.GameObject);
			isTypeGameObject = true;
			break;
		};
		
		if(mainAsset == null)
		{
			try
			{
				request = bundle.LoadAsync(source.Replace(".unity3d", ""), otype); 
			}
			catch (Exception e)
			{
				Common.ERROR_MSG("Asset::_InstantiateObj: " + e.ToString());
			}
		}
		
		UnityEngine.Object go = null;

		if(request != null)
		{
			yield return request;
			
			mainAsset = request.asset;
			isLoaded = true;
		}
		
		if(mainAsset != null)
		{
			if(setposdir == true) 
			{
				go = UnityEngine.GameObject.Instantiate(mainAsset, position, Quaternion.Euler(rotation));
				go.name = name;
				
				if((type == TYPE.WORLD_OBJ || type == TYPE.NORMAL) && layerName != "" && layerName != "Default")
					((UnityEngine.GameObject)go).layer = LayerMask.NameToLayer(layerName);
				
				if(isTypeGameObject == true)
					((UnityEngine.GameObject)go).transform.localScale = scale;
				
				Common.DEBUG_MSG("Asset::Instantiate: " + name + "(source=" + source + "), pos:" + position + ", dir:" + rotation + ", scale:" + scale + ", layer:" + layerName);
			}
			else
			{
				go = UnityEngine.GameObject.Instantiate(mainAsset);
				go.name = name;   
				
				if((type == TYPE.WORLD_OBJ || type == TYPE.NORMAL) && layerName != "" && layerName != "Default")
					((UnityEngine.GameObject)go).layer = LayerMask.NameToLayer(layerName);
				
				if(isTypeGameObject == true)
				{
					Common.DEBUG_MSG("Asset::Instantiate(auto): " + name + ", pos:" + ((UnityEngine.GameObject)go).transform.position + ", dir:" + 
						((UnityEngine.GameObject)go).transform.rotation + ", scale:" + ((UnityEngine.GameObject)go).transform.localScale);
				}
				else{
					Common.DEBUG_MSG("Asset::Instantiate: " + name );
				}
			}
		}
		else
		{
			bundle.LoadAll();
			Common.ERROR_MSG("Asset::Instantiate: build [" + name + "], source[" + 
				source.Replace(".unity3d", "") + "], realbundleName[" + bundle.mainAsset.name + "] is failed!");
		}

		cb.onAssetAsyncLoadObjectCB(name, go, this);
	}
	
	public void Instantiate(AssetAsyncLoadObjectCB cb, string name, Vector3 position, Vector3 rotation, Vector3 scale)
	{
		loader.inst.StartCoroutine(_InstantiateObj(cb, name, position, rotation, scale, true));
	}
	
	public void Instantiate(AssetAsyncLoadObjectCB cb, string name)
	{
		loader.inst.StartCoroutine(_InstantiateObj(cb, name, Vector3.zero, Vector3.zero, Vector3.zero, false));
	}
	
	public void onLoadCompleted()
	{
		if(isLoaded != false)
			return;
		
		loading = false;
		for(int i=0; i < refs.Count; i++)
		{
			string refname = refs[i];
			Scene scene = null;
			if(!loader.inst.scenes.TryGetValue(loader.inst.currentSceneName, out scene))
			{
				Common.ERROR_MSG("Asset::onLoadCompleted: not found scene(" + loader.inst.currentSceneName + ")!");
				return;
			}
			
			SceneObject sobj = null;
			if(!scene.objs.TryGetValue(refname, out sobj))
			{
				Common.ERROR_MSG("Asset::onLoadCompleted: scene(" + loader.inst.currentSceneName + ") not found obj(" + refname + ")!");
				return;
			}
			
			if(sobj != null)
				sobj.Instantiate();
		}
		
		Common.DEBUG_MSG("Asset::onLoadCompleted:" + source + " " + "ref(" + refs.Count + ")!");
		refs.Clear();
	}
}  


