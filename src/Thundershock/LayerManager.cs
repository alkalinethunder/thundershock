using System;
using System.Collections.Generic;
using System.Linq;
using Thundershock.Core;
using Thundershock.Core.Input;

namespace Thundershock
{
    public class LayerManager
    {
        private List<Layer> _layers = new();
        private int _layerIndex;
        private int _overlayIndex;
        
        private GraphicalAppBase _app;

        public LayerManager(GraphicalAppBase app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
        }

        public bool HasLayer<T>() where T : Layer
        {
            return _layers.Any(x => x is T);
        }
        
        public void FireMouseDown(MouseButtonEventArgs args)
        {
            for (var i = _layers.Count - 1; i >= 0; i--)
            {
                var layer = _layers[i];
                if (layer.MouseDown(args))
                    break;
            }
        }

        public void FireMouseUp(MouseButtonEventArgs args)
        {
            for (var i = _layers.Count - 1; i >= 0; i--)
            {
                var layer = _layers[i];
                if (layer.MouseUp(args))
                    break;
            }
        }
        
        public void FireMouseMove(MouseMoveEventArgs args)
        {
            for (var i = _layers.Count - 1; i >= 0; i--)
            {
                var layer = _layers[i];
                if (layer.MouseMove(args))
                    break;
            }
        }
        
        public void FireMouseScroll(MouseScrollEventArgs args)
        {
            for (var i = _layers.Count - 1; i >= 0; i--)
            {
                var layer = _layers[i];
                if (layer.MouseScroll(args))
                    break;
            }
        }

        
        public void FireKeyDown(KeyEventArgs args)
        {
            for (var i = _layers.Count - 1; i >= 0; i--)
            {
                var layer = _layers[i];
                if (layer.KeyDown(args))
                    break;
            }
        }
        
        public void FireKeyUp(KeyEventArgs args)
        {
            for (var i = _layers.Count - 1; i >= 0; i--)
            {
                var layer = _layers[i];
                if (layer.KeyUp(args))
                    break;
            }
        }
        
        public void FireKeyChar(KeyCharEventArgs args)
        {
            for (var i = _layers.Count - 1; i >= 0; i--)
            {
                var layer = _layers[i];
                if (layer.KeyChar(args))
                    break;
            }
        }
        
        public void PushLayer(Layer layer)
        {
            _layers.Insert(_layerIndex, layer);
            _layerIndex++;
            _overlayIndex++;

            layer.Initialize(this._app);
        }

        public Layer PopLayer()
        {
            if (_layerIndex > 0)
            {
                var lastLayer = _layers[_layerIndex - 1];
                lastLayer.Unload();
                _layers.Remove(lastLayer);
                _layerIndex--;
                _overlayIndex--;
                return lastLayer;
            }

            return null;
        }

        public void PushOverlay(Layer layer)
        {
            _layers.Insert(_overlayIndex, layer);
            _layerIndex++;
            _overlayIndex++;
            
            layer.Initialize(this._app);
        }

        public Layer PopOverlay()
        {
            if (_overlayIndex > _layerIndex)
            {
                var lastLayer = _layers.Last();
                lastLayer.Unload();
                _layers.Remove(lastLayer);
                _overlayIndex--;
                return lastLayer;
            }

            return null;
        }

        public void Update(GameTime gameTime)
        {
            foreach (var layer in _layers.ToArray())
            {
                layer.Update(gameTime);
            }
        }

        public void Render(GameTime gameTime)
        {
            foreach (var layer in _layers.ToArray())
            {
                layer.Render(gameTime);
            }
        }

        public void RemoveLayer(Layer layer)
        {
            var index = _layers.IndexOf(layer);
            
            _layers.RemoveAt(index);

            if (index <= _layerIndex)
                _layerIndex--;

            if (index <= _overlayIndex)
                _overlayIndex--;
        }
    }
}