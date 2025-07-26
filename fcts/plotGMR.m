function [sig_up,sig_down] = plotGMR(Mu, Sigma, color, valAlpha)
% This function displays the parameters of a Gaussian Mixture Model (GMM).
% Inputs -----------------------------------------------------------------
%   o Mu:           D x K array representing the centers of K Gaussians.
%   o Sigma:        D x D x K array representing the covariance matrices of K Gaussians.
%   o color:        3 x 1 array representing the RGB color to use for the display.
%   o valAlpha:     transparency factor (optional).
%
% Writing code takes time. Polishing it and making it available to others takes longer! 
% If some parts of the code were useful for your research of for a better understanding 
% of the algorithms, please reward the authors by citing the related publications, 
% and consider making your own research available in this way.
%
% @article{Calinon15,
%   author="Calinon, S.",
%   title="A Tutorial on Task-Parameterized Movement Learning and Retrieval",
%   journal="Intelligent Service Robotics",
%   year="2015"
% }
%
% Copyright (c) 2015 Idiap Research Institute, http://idiap.ch/
% Written by Sylvain Calinon, http://calinon.ch/
% 
% This file is part of PbDlib, http://www.idiap.ch/software/pbdlib/
% 
% PbDlib is free software: you can redistribute it and/or modify
% it under the terms of the GNU General Public License version 3 as
% published by the Free Software Foundation.
% 
% PbDlib is distributed in the hope that it will be useful,
% but WITHOUT ANY WARRANTY; without even the implied warranty of
% MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
% GNU General Public License for more details.
% 
% You should have received a copy of the GNU General Public License
% along with PbDlib. If not, see <http://www.gnu.org/licenses/>.


nbStates = size(Mu,2);
nbDrawingSeg = 35;
darkcolor = color*0.5; %max(color-0.5,0);
t = linspace(-pi, pi, nbDrawingSeg);

h=[];
sig_up=[];
sig_down=[];
for i=1:nbStates
	%R = real(sqrtm(1.0.*Sigma(:,:,i)));
	[V,D] = eig(Sigma(:,:,i));
	R = real(V*D.^.5);
	X = R * [cos(t); sin(t)] + repmat(Mu(:,i), 1, nbDrawingSeg);
	if nargin>3 %Plot with alpha transparency
%		h = [h patch(X(1,:), X(2,:), color, 'lineWidth', 1, 'EdgeColor', darkcolor, 'facealpha', valAlpha,'edgealpha', valAlpha)];
% 		sig_up(:,i)= R * [cos(pi/2); sin(pi/2)]+Mu(:,i);
%         sig_down(:,i)= R * [cos(3*pi/2); sin(3*pi/2)]+Mu(:,i);
        [m,index]=max(X(2,:));
        sig_up(:,i)=[X(1,index);m];
        [m,index]=min(X(2,:));
        sig_down(:,i)=[X(1,index);m];
%         sig_up(1,i)= Mu(1,i);
%         sig_up(2,i)= R +Mu(2,i);
%         sig_down(1,i)= Mu(1,i);
%         sig_down(2,i)= -R +Mu(2,i);
        if sig_up(2,i)<sig_down(2,i)
            c=sig_up(:,i);
            sig_up(:,i)=sig_down(:,i);
            sig_down(:,i)=c;
        end
        %MuTmp = [cos(t); sin(t)] * 0.3 + repmat(Mu(:,i),1,nbDrawingSeg);
		%h = [h patch(MuTmp(1,:), MuTmp(2,:), darkcolor, 'LineStyle', 'none', 'facealpha', valAlpha)];
% 		h = [h plot(Mu(1,:), Mu(2,:), '.', 'markersize', 6, 'color', darkcolor)];
	else %Plot without transparency
		h = [h patch(X(1,:), X(2,:), color, 'lineWidth', 1, 'EdgeColor', darkcolor)];
        sig_up(:,i)= R * [cos(pi/2); sin(pi/2)]+Mu(:,i);
        sig_down(:,i)= R * [cos(3*pi/2); sin(3*pi/2)]+Mu(:,i);
%         sig_up(1,i)= Mu(1,i);
%         sig_up(2,i)= R +Mu(2,i);
%         sig_down(1,i)= Mu(1,i);
%         sig_down(2,i)= -R +Mu(2,i);
        if sig_up(2,i)<sig_down(2,i)
            c=sig_up(:,i);
            sig_up(:,i)=sig_down(:,i);
            sig_down(:,i)=c;
        end
	end
end
