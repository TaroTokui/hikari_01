#region usings
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	public struct Particle
	{
		public int index;
		public Vector2D pos;
		public Vector2D vel;
		public Vector2D acc;
		public Vector4D color;
		public Vector2D size;
		public float age;
		public float life;
		public int state;
		public bool isActive;
		public bool isDead;
	}
	
	#region PluginInfo
	[PluginInfo(Name = "InteractiveParticle", Category = "Value", Help = "Basic template with one value in/out", Tags = "")]
	#endregion PluginInfo
	public class ValueInteractiveParticleNode : IPluginEvaluate
	{
		public const int MAX_PARTICLE_COUNT = 1000;
	
		#region fields & pins
		[Input("Position", DefaultValue = 1.0)]
		public ISpread<Vector2D> FInputPos;
		[Input("Velocity", DefaultValue = 1.0)]
		public ISpread<Vector2D> FInputVel;
		[Input("Acceleration", DefaultValue = 1.0)]
		public ISpread<Vector2D> FInputAcc;
		[Input("Size", DefaultValue = 1.0)]
		public ISpread<Vector2D> FInputSize;
//		[Input("MaxCount", DefaultValue = 100)]
//		public ISpread<int> FMaxCount;
		[Input("Life", DefaultValue = 100)]
		public ISpread<int> Life;
		[Input("EmitCount", DefaultValue = 1)]
		public ISpread<int> EmitCount;
		[Input("Emit", DefaultValue = 0)]
		public ISpread<bool> FEmit;
		[Input("Reset", DefaultValue = 0)]
		public ISpread<bool> FReset;

		[Output("Position")]
		public ISpread<Vector2D> FOutputPos;
		[Output("Size")]
		public ISpread<Vector2D> FOutputSize;
		[Output("IsDead")]
		public ISpread<bool> FOutputDead;
		[Output("Count")]
		public ISpread<int> FOutputCount;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		List<Particle> particles;
		
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			int n = FInputPos.SliceCount;
//			int count = particles.Count;
			
			// initialize
			if (particles == null) {
				particles = new List<Particle>();
				for(int i=0; i<MAX_PARTICLE_COUNT; i++)
				{
					Particle p = new Particle();
					p.index = i;
					p.pos = FInputPos[0];
					p.vel = FInputVel[0];
					p.acc = FInputAcc[0];
					p.color = new Vector4D(1,1,1,1);
					p.size = FInputSize[0];
					p.age = 0;
					p.life = 0;
					p.isActive = false;
					p.isDead = false;
					particles.Add(p);
				}
			}
			
			// add particle
			if(FEmit[0]){
				for(int i=0; i<MAX_PARTICLE_COUNT; i++)
				{
					Particle p = particles[i];
					if(!p.isActive)
					{
						p.pos = FInputPos[0];
						p.vel = FInputVel[0];
						p.acc = FInputAcc[0];
						p.size = FInputSize[0];
						p.age = 0;
						p.life = Life[0] * 10;
						p.isActive = true;
						p.isDead = false;
						particles[i] = p;
						EmitCount[0]--;
						if(EmitCount[0] <= 0) break;
					}
				}
			}
			
			// reset particle state
			if(FReset[0]){
				for(int i=0; i<MAX_PARTICLE_COUNT; i++)
				{
					Particle p = particles[i];
					p.isActive = false;
					p.isDead = false;
					particles[i] = p;
				}
			}
			
			// update
			int counter = 0;
			for(int i=0; i<MAX_PARTICLE_COUNT; i++)
			{
				Particle p = particles[i];
				p.vel += p.acc;
				p.pos += p.vel;
				p.age++;
				if(p.age > p.life)
				{
					p.isDead = true;
				}
				particles[i] = p;
				if(particles[i].isActive) counter++;
			}
			
			// output
			FOutputCount[0] = counter;
			FOutputPos.SliceCount = counter;
			FOutputSize.SliceCount = counter;
			counter = 0;
			for (int i = 0; i < MAX_PARTICLE_COUNT; i++)
			{
				if(!particles[i].isActive) continue;
				
				Particle p = particles[i];
				p.vel += p.acc;
				p.pos += p.vel;
				p.age++;
				if(p.isDead)
				{
					p.isActive = false;
					FOutputDead[counter] = true;
				}else{
					FOutputDead[counter] = false;
				}
				FOutputPos[counter] = p.pos;
				FOutputSize[counter] = p.size;
				particles[i] = p;
				FLogger.Log(LogType.Debug, "id: " + p.index);
				counter++;
				//FInputPos[i] * 2;
//				if(particles[i].life < 0)
//				{
//					FOutputDead[i] = true;				
//				}else{
//					FOutputDead[i] = false;	
//				}
			}


			//FLogger.Log(LogType.Debug, "hi tty!");
		}
	}
}
