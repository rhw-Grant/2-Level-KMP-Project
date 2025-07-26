function [sampleData,kmpTraj,quatRef]=kmp_insert_point(oldr,quatRef,inserttime,tau,dt,flag)
%%%最终版本
% addpath('../fcts/');
len=round(tau / dt);%表格一共有多少个点
%%参数解释
%oldr 处理前路径
%quatRef GMM的基础路径

%flag表示角度减小的方向
%flag=0时，逆向减小
%flag=1时，正向减小



%我就是不写标注，反正我看得懂
% 设置KMP模型参数
lamda=0.01;%0.05
kh=0.1;
dim=1;

via_var=1E-10*eye(2);
via_time=[];

% timeplot=linspace(0, tau, len);
% plot(timeplot,oldr(2,:),"-",'linewidth',4);
% hold on;


if flag==0
    for i=1:length(inserttime)
        newpoint=inserttime(i)/dt%插入的数据是原本路径的哪个点
        via_time(i)=inserttime(i);   % desired time
        %需要将newpoint转换成int类型，不然数组无法进行识别
        via_point(1,i)=oldr(2,int16(newpoint))-1.5;
        via_point(2,i)=(oldr(2,int16(newpoint)+1)-oldr(2,int16(newpoint)))/dt; 
    end
elseif flag==1
    for i=1:length(inserttime)
        newpoint=inserttime(i)/dt%插入的数据是原本路径的哪个点
        via_time(i)=inserttime(i);   % desired time
        %需要将newpoint转换成int类型，不然数组无法进行识别
        via_point(1,i)=oldr(2,int16(newpoint))+1.5;
        via_point(2,i)=(oldr(2,int16(newpoint)+1)-oldr(2,int16(newpoint)))/dt; 
    end
end

%% Update the reference trajectory using transformed desired points
interval=5; % speed up the computation
num=round(len/interval)+1;
for i=1:num
    if i==1 index=1;
    else index=(i-1)*interval;
    end
    sampleData(i).t=quatRef(index).t;
    sampleData(i).mu=quatRef(index).mu;
    sampleData(i).sigma=quatRef(index).sigma; 
end

for i=1:length(inserttime)
    [sampleData,num] = kmp_insertPoint(sampleData,num,via_time(i),via_point(:,i),via_var);
end
%%更新高斯拟合路径
for i=1:num
    if i==1 index=1;
    else index=(i-1)*interval;
    end
    quatRef(index).t=sampleData(i).t;
    quatRef(index).mu=sampleData(i).mu;
    quatRef(index).sigma=sampleData(i).sigma; 
end

%% KMP prediction
Kinv = kmp_estimateMatrix_mean(sampleData,num,kh,lamda,dim);

for index=1:len
    t=index*dt;
    mu=kmp_pred_mean(t,sampleData,num,kh,Kinv,dim);    
    kmpTraj(1,index)=t;
    kmpTraj(2:3,index)=mu;
end

% timeplot=linspace(0, tau, len);
% plot(timeplot,kmpTraj(2,:),"-",'linewidth',3);
% hold on;
% for i=1:length(inserttime)
% plot(via_time(i),via_point(1,i),"o",'linewidth',3);
% hold on
% end

end

