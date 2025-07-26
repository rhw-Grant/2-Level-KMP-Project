using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matlab_connect
{

    internal class ForceOrDegree
    {
        private double torqueX;
        private double torqueY;
        private double torqueZ;
        private double torqueXM;
        private double torqueYM;
        private double torqueZM;
        double[] torque=new double[6];

        public void setForce(double torqueX, double torqueY, double torqueZ, double torqueXM, double torqueYM, double torqueZM)
        {
            this.torqueX = torqueX;
            this.torqueY = torqueY;
            this.torqueZ = torqueZ;
            this.torqueXM = torqueXM;
            this.torqueYM = torqueYM;
            this.torqueZM = torqueZM;
            torque[0] = this.torqueX;
            torque[1] = this.torqueY;
            torque[2] = this.torqueZ;
            torque[3] = this.torqueXM;
            torque[4] = this.torqueYM;
            torque[5] = this.torqueZM;
        }

        public double[] getForce()
        {
            return torque;
        }
    }
}
