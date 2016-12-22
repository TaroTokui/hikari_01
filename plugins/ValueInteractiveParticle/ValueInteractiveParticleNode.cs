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
		public const int MAX_PARTICLE_COUNT = 500;
	
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
		[Input("DeleteID", DefaultValue = -1)]
		public ISpread<int> DeleteID;
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
		[Output("ID")]
		public ISpread<int> FOutputID;
		[Output("IsDead")]
		public ISpread<bool> FOutputDead;
		[Output("Count")]
		public ISpread<int> FOutputCount;
		[Output("Life")]
		public ISpread<double> FOutputLife;

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
				int addCount = EmitCount[0];
				for(int i=0; i<MAX_PARTICLE_COUNT; i++)
				{
					if(particles[i].isActive) continue;
					Particle p = particles[i];
					p.pos = FInputPos[0];
					p.vel = FInputVel[0];
					p.acc = FInputAcc[0];
					p.size = FInputSize[0];
					p.age = 0;
					p.life = Life[0] * 10;
					p.isActive = true;
					p.isDead = false;
					particles[i] = p;
					addCount--;
					if(addCount <= 0) break;
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
			int deleteCount = DeleteID.SliceCount;
			for(int i=0; i<MAX_PARTICLE_COUNT; i++)
			{
				if(!particles[i].isActive) continue;
				Particle p = particles[i];
				p.vel += p.acc;
				p.pos += p.vel;
				p.age++;
				particles[i] = p;
				
				// check colision
//				for(int j=0; j<deleteCount; j++){
//					if(p.index == DeleteID[j])
//					{
//						p.isDead = true;
//					}
//				}
				
				counter++;
//				if(particles[i].isActive) counter++;
			}
			
			// output
			FOutputCount[0] = counter;
			FOutputPos.SliceCount = counter;
			FOutputSize.SliceCount = counter;
			FOutputID.SliceCount = counter;
			FOutputLife.SliceCount = counter;
			FOutputDead.SliceCount = counter;
			counter = 0;
			for (int i = 0; i < MAX_PARTICLE_COUNT; i++)
			{
				if(!particles[i].isActive) continue;
				
				Particle p = particles[i];
				
				if(p.isDead)
				{
					p.isActive = false;
					FOutputDead[counter] = false;
				}else{
					for(int j=0; j<deleteCount; j++){
						if(p.age >= p.life || p.index == DeleteID[j])
						{
							p.isDead = true;
							FOutputDead[counter] = true;
						}else{
							FOutputDead[counter] = false;
						}
					}
				}
				FOutputPos[counter] = p.pos;
				FOutputSize[counter] = p.size;
				FOutputID[counter] = p.index;
				FOutputLife[counter] = 1.0 - p.age / p.life;
//				FLogger.Log(LogType.Debug, "id: " + p.index);
				particles[i] = p;
				counter++;
			}
		}
	}
}
