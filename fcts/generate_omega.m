function [omega, domega, t] = generate_omega(dataout, tau, len)
%-------------------------------------------------------------------------
% Generate quaternion data, angular velocities and accelerations for DMP
% learning
% Copyright (C) Fares J. Abu-Dakka  2013
%计算角速度和角加速度
% Normalize quaternions
    t = linspace(0, tau, len);
    for i = 1:len
        tmp = quat_norm(dataout(i));
        dataout(i).s = dataout(i).s / tmp;
        dataout(i).v = dataout(i).v / tmp;
        qq(:,i) = [dataout(i).s; dataout(i).v];
    end
    % Calculate derivatives
    for j = 1:4
        dqq(j,:) = gradient(qq(j,:), t);
    end
    % Calculate omega and domega
    for i = 1:len
        dq.s = dqq(1,i);
        for j = 1:3
            dq.v(j,1) = dqq(j+1,i);
        end
        omega_q = quat_mult(dq, quat_conjugate(dataout(i)));
        omega(:,i) = 2*omega_q.v;
    end
    for j = 1:3
        domega(j,:) = gradient(omega(j,:), t);
    end
    omega(:,1) = [0; 0; 0];
    omega(:,len) = [0; 0; 0];
    domega(:,1) = [0; 0; 0];
    domega(:,len) = [0; 0; 0];
end