using System.Collections.Generic;
using UnityEngine;

namespace Battlehub.RTHandles
{
    public interface ICustomOutlineRenderersCache {
        List<ICustomOutlinePrepass> GetOutlineRendererItems();
    }
}