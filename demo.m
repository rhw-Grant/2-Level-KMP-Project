clc;clear all;

addpath('D:\Project\matlab_work\kmp_route_test\octave-jupyter-notebooks');
addpath('D:/Project/matlab_work/kmp_route_test/route_process/');
addpath('.\function\');
addpath( 'D:\Project\matlab_work\kmp_route_test\fcts');

tau=100;
dt=0.1;
N=7;

path='D:\Project\matlab_work\kmp_route_test\route_process\数据汇总\Subject1(dongderui)\degree\route_x.csv';
[Data_x,newdata_x]=load_route(path,tau,dt,N);
path='D:\Project\matlab_work\kmp_route_test\route_process\数据汇总\Subject1(dongderui)\degree\route_y.csv';
[Data_y,newdata_y]=load_route(path,tau,dt,N);
path='D:\Project\matlab_work\kmp_route_test\route_process\数据汇总\Subject1(dongderui)\degree\route_z.csv';
[Data_z,newdata_z]=load_route(path,tau,dt,N);
quatRef(1,:)=generate_gmm_route(Data_x,tau,dt);
quatRef(2,:)=generate_gmm_route(Data_y,tau,dt);
quatRef(3,:)=generate_gmm_route(Data_z,tau,dt);
% quatRef(1,:)=load_gmm_route("D:/Project/matlab_work/kmp_route_test/route_process/vicon_route_process/gmm_route5.mat",1);
% quatRef(2,:)=load_gmm_route("D:/Project/matlab_work/kmp_route_test/route_process/vicon_route_process/gmm_route5.mat",2);
% quatRef(3,:)=load_gmm_route("D:/Project/matlab_work/kmp_route_test/route_process/vicon_route_process/gmm_route5.mat",3);
len=round(tau / dt);%表格一共有多少个点
interval=5;
num=round(len/interval)+1;
for i=1:3
    trajAda(3*i-2:3*i,:)=generate_kmp_route(quatRef(i,:),tau,dt);
end
plot(trajAda(1,:),trajAda(2,:),'-','LineWidth',1);
hold on;

% path_x='C:\Users\13753\Desktop\周宇师兄的踝康复系统\并联踝康复机器人控制系统\route\route1_1.csv';
% data_x=csvread(path_x,1,0)';
% trajAda(2,:)=data_x(2,:);
% for i=1:1000
%     quatRef(1,i).mu(1)=trajAda(2,i);
% end



inserttime=[30,45,55,70];
oldr=trajAda(1:3,:);
[sampleData,trajAda(1:3,:),quatRef(1,:)]= double_KMP(oldr,quatRef(1,:),inserttime,tau,dt,0,2,0.1);
% inserttime=[30,45,55,70];
oldr=trajAda(1:3,:);
plot(trajAda(1,:),trajAda(2,:),'-','LineWidth',1);
hold on;
plot(inserttime,trajAda(2,inserttime*10),'o','LineWidth',2.5);
hold on;
col={'time','degreeX', 'degreeY', 'degreeZ'}; 
hold on;

result_table=table(trajAda(1,:)',trajAda(2,:)',trajAda(5,:)',trajAda(8,:)','VariableNames',col);

