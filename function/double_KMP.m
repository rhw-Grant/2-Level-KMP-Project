function [sampleData,kmpTraj,quatRef]= double_KMP(oldr,quatRef,inserttime,tau,dt,flag,changeAngle,lamda)
% addpath('D:/Project/matlab_work/kmp_route_test/fcts/');
% addpath('D:/Project/matlab_work/kmp_route_test/Lamda_Change_process/');
len=round(tau / dt);%表格一共有多少个点
%%参数解释
%oldr 处理前路径
%quatRef GMM的基础路径

%flag表示角度减小的方向
%flag=0时，逆向减小
%flag=1时，正向减小

via_var=0.75E-10*eye(2);

[sampleData,kmpTraj]=kmp_insert_point1(oldr,quatRef,inserttime,tau,dt,flag,changeAngle,lamda);

% plot(kmpTraj(1,:),kmpTraj(2,:),'LineWidth',2.5);
% hold on;
% plot(inserttime,kmpTraj(2,inserttime*10),'o','LineWidth',2.5);
% hold on;
% for i=1:num
%     if i==1 index=1;
%     else index=(i-1)*interval;
%     end
%     quatRef(index).t=sampleData(i).t;
%     quatRef(index).mu=sampleData(i).mu;
%     quatRef(index).sigma=sampleData(i).sigma; 
% end

for i=1:length(inserttime)
    temp(i).sigma=quatRef(inserttime(i)*10).sigma;
end

for i=1:1000
    quatRef(i).mu(1)=kmpTraj(2,i);
end

for i=1:length(inserttime)
    quatRef(inserttime(i)*10).sigma=via_var;
end


kmpTraj=generate_kmp_route1(quatRef,tau,dt,0.1);
for i=1:length(inserttime)
    quatRef(inserttime(i)*10).sigma=temp(i).sigma;
end

end
