# Readme


 1. **·** **Folder C#Using****：****The relevant codes adopted in the control of the PARR upper computer**

	generate_referance_trajectory.txt：Reference trajectories were generated through GMR based on the physician trajectories collected by VICON.

	generate_imatation_trajectory.txt：The imitation trajectory is generated through 2-level-KMP based on the reference trajectory.

	patient_in_loop_optimizer.txt：Perform normalization operation on the torque information fed back by the Six-axis circular load cell, calculate .and generate the z-score, and conduct point insertion operation.

	inset_point_process.txt：The specific process of the point insertion operation.

 2. **·** **Folder DLL****：****Winform calls matlab for 2-level-KMP operation required DLL files.**

	double_KMP.dll：double KMP process operation.

	generate_gmm_route.dll：The GMM model and the GMR reference trajectory are generated based on the physician experience trajectory dataset.

	generate_kmp_route.dll：Single KMP process poeration.

	kmp_insert_point.dll：KMP Point insertion operation.

	Kmp_route_show.dll：The physician imitation trajectory display function.

	load_gmm_route.dll：Loading the generated GMM model.

	load_route.dll：Load CSV files for physician experience trajectories.

	MWArray.dll：The output matrix of matlab data type used in C#.

	show_pics.dll：Showing the physician experience trajectory.

 3. **·** **Folder fcts****：****The basic function performed in 2-level-KMP algorithm by matlab.**
 4. **·** **Folder function****：****The main function performed in 2-level-KMP algorithm by matlab.**

	BayesianKMP.py：Using Bayesian optimizer to selects the KMP hyperparameters

	double_KMP.m：double KMP process operation.

	generate_gmm_route.m：Gernerating GMR reference trajectory.

	generate_kmp_route.m：Single KMP process poeration.

	kmp_insert_point.m：KMP Point insertion operation.

	load_route.m：Load CSV files for physician experience trajectories.

<!--stackedit_data:
eyJoaXN0b3J5IjpbODI0NzcwMTEzXX0=
-->
