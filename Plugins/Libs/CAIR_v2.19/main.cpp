//=========================================================================================================//
//CAIR Example Suite

//=========================================================================================================//
//Copyright (C) 2009 Joseph Auman (brain.recall@gmail.com)

//=========================================================================================================//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA

//=========================================================================================================//
//This is a simple test program for CAIR.
//=========================================================================================================//

#include <iostream>
#include <ctime>
#include "CAIR.h"
#include "CAIR_CML.h"
#include "./EasyBMP/EasyBMP.h"

enum Arg_Param { INPUT_FILENAME = 0, GOAL_X, GOAL_Y, OUTPUT_FILENAME, RESULT_TYPE, CONVOLUTION, WEIGHT_FILENAME, WEIGHT_SCALE, ENERGY_TYPE, THREAD_COUNT };

using namespace std;

//=========================================================================================================//
void BMP_to_CML( BMP * Source, CML_color * Dest )
{
	(*Dest).D_Resize( (*Source).TellWidth(), (*Source).TellHeight() );

	for( int y = 0; y < (*Source).TellHeight(); y++ )
	{
		for( int x = 0; x < (*Source).TellWidth(); x++ )
		{
			(*Dest)(x,y).alpha = (*Source)(x,y)->Alpha;
			(*Dest)(x,y).red = (*Source)(x,y)->Red;
			(*Dest)(x,y).green = (*Source)(x,y)->Green;
			(*Dest)(x,y).blue = (*Source)(x,y)->Blue;
		}
	}
}

//=========================================================================================================//
void CML_to_BMP( CML_color * Source, BMP * Dest )
{
	(*Dest).SetSize( (*Source).Width(), (*Source).Height() );

	for( int y = 0; y < (*Source).Height(); y++ )
	{
		for( int x = 0; x < (*Source).Width(); x++ )
		{
			(*(*Dest)(x,y)).Alpha = (*Source)(x,y).alpha;
			(*(*Dest)(x,y)).Red = (*Source)(x,y).red;
			(*(*Dest)(x,y)).Green = (*Source)(x,y).green;
			(*(*Dest)(x,y)).Blue = (*Source)(x,y).blue;
		}
	}
}

//=========================================================================================================//
//simple weight tester
//_NOT_ intended as the means to set Weights. Best to use a graphical interface for that.
//top_x/y define the top left corner of the rectangle, bot_x/y define the bottom right corner.
void Weight_Rectangle( CML_int * Weights, int top_x, int top_y, int bot_x, int bot_y, int weight )
{
	int i = 0, j = 0;

	for( i = top_x; i < (top_x + (bot_x-top_x)); i++ )
	{
		for( j = top_y; j < (top_y + (bot_y-top_y)); j++ )
		{
			(*Weights)(i,j) = weight;
		}
	}
}

//=========================================================================================================//
//search argParameter
char * getArgParameter( Arg_Param arg, int argc, char * argv[] )
{
	char * sToBeFind = NULL;

	switch(arg)
	{
	case INPUT_FILENAME :
		sToBeFind = "-I";
		break;
		
	case OUTPUT_FILENAME :
		sToBeFind = "-O";
		break;
		
	case GOAL_X :
		sToBeFind = "-X";
		break;
		
	case GOAL_Y :
		sToBeFind = "-Y";
		break;
		
	case RESULT_TYPE :
		sToBeFind = "-R";
		break;

	case CONVOLUTION :
		sToBeFind = "-C";
		break;			

	case WEIGHT_FILENAME :
		sToBeFind = "-W";
		break;
		
	case WEIGHT_SCALE :
		sToBeFind = "-S";
		break;
	case ENERGY_TYPE :
		sToBeFind = "-E";
		break;
	case THREAD_COUNT :
		sToBeFind = "-T";
		break;
	}

	for ( int i = 1 ; i < argc ; i++ )//minus one, because we return the next
	{
		if( memcmp ( argv[i], sToBeFind, 2 ) == 0 )//find if
			return argv[i+1]; 
	}
	return NULL;
}

//=========================================================================================================//
void Print_Usage()
{
	cout << "CAIR CLI Usage: cair -I <input_file>" << endl;
	cout << "Other options:" << endl;
	cout << "  -O <output_file>" << endl;
	cout << "      Default: Dependent on operation" << endl;
	cout << "  -W <weight_file>" << endl;
	cout << "      Bitmap with: Black- no weight" << endl;
	cout << "                   Green- Protect weight" << endl;
	cout << "                   Red- Remove weight" << endl;
	cout << "      Default: Weights are all zero" << endl;
	cout << "  -S <weight_scale>" << endl;
	cout << "      Default: 100,000" << endl;
	cout << "  -X <goal_x>" << endl;
	cout << "      Default: Source image width" << endl;
	cout << "  -Y <goal_y>" << endl;
	cout << "      Default: Source image height" << endl;
	cout << "  -R <expected_result>" << endl;
	cout << "      CAIR: 0" << endl;
	cout << "      Grayscale: 1" << endl;
	cout << "      Edge: 2" << endl;
	cout << "      Vertical Energy: 3" << endl;
	cout << "      Horizontal Energy: 4" << endl;
	cout << "      Removal: 5" << endl;
	cout << "      CAIR_HD: 6" << endl;
	cout << "      Default: CAIR" << endl;
	cout << "  -C <convoluton_type>" << endl;
	cout << "      Prewitt: 0" << endl;
	cout << "      V1: 1" << endl;
	cout << "      V_SQUARE: 2" << endl;
	cout << "      Sobel: 3" << endl;
	cout << "      Laplacian: 4" << endl;
	cout << "      Default: Prewitt" << endl << endl;
	cout << "  -E <energy_type>" << endl;
	cout << "      Backward: 0" << endl;
	cout << "      Forward: 1" << endl;
	cout << "      Default: Backward" << endl;
	cout << "  -T <thread_count>" << endl;
	cout << "      Default : CAIR_NUM_THREADS (" << CAIR_NUM_THREADS << ")" << endl;
	cout << "http://sourceforge.net/projects/c-a-i-r/" << endl;
}

//=========================================================================================================//
//callback test function
bool cancel_callback( float percent_done )
{
	cout << "Percent done: " << (int)(percent_done * 100) << "%" << endl;

	return true;
}

//=========================================================================================================//
//simple tester
int main( int argc, char * argv[] )
{
	cout << "Welcome to the Content Aware Image Resizer (CAIR) Test Application!" << endl;

	BMP Image;
	BMP Resized;
	time_t start;
	time_t end;
	
	char * output_filename;
	char * input_filename;
	char * weight_filename;
	int goal_x;
	int goal_y;
	int weight_scale;
	int result_type;//type of result : 0 image, 1 grayscale, 2 edge, 3 V-energy
	CAIR_convolution convolution;
	CAIR_energy ener;

	//set parameters
	input_filename = getArgParameter( INPUT_FILENAME , argc, argv);
	if( input_filename == NULL )
	{
		cout << "Error: Input image required" << endl;
		Print_Usage();
		return 0;
	}
	
	//set image
	Image.ReadFromFile( input_filename );
	CML_color Source( Image.TellWidth(), Image.TellHeight() );
	CML_color Dest( Image.TellWidth(), Image.TellHeight() );

	//default parameters
	char * temp = NULL;
	
	//the -X param
	temp = getArgParameter( GOAL_X, argc, argv );
	if( temp != NULL )
	{
		goal_x = atoi( temp );
	}
	else
	{
		goal_x = Image.TellWidth();
	}
	
	//the -Y param
	temp = getArgParameter( GOAL_Y, argc, argv );
	if( temp != NULL )
	{
		goal_y = atoi( temp );
	}
	else 
	{
		goal_y = Image.TellHeight();
	}
	
	//the -R param
	temp = getArgParameter( RESULT_TYPE, argc, argv );
	if( temp != NULL )
	{
		result_type = atoi( temp );
	}
	else 
	{
		result_type = 0;
	}

	//the -C param
	temp = getArgParameter( CONVOLUTION, argc, argv );
	if( temp != NULL )
	{
		convolution = (CAIR_convolution)atoi( temp );
	}
	else
	{
		convolution = PREWITT;
	}

	//the -O param
	output_filename = getArgParameter( OUTPUT_FILENAME, argc, argv );
	if( output_filename == NULL )
	{
		switch( result_type )
		{
		case 0 :
			output_filename = "output.bmp"; 
			break;
		case 1 :
			output_filename = "out_grayscale.bmp";
			break;
		case 2 :
			output_filename = "out_edge.bmp";
			break;
		case 3 :
			output_filename = "out_energy_V.bmp";
			break;
		case 4 :
			output_filename = "out_energy_H.bmp";
			break;
		case 5 :
			output_filename = "out_Remove.bmp";
			break;
		case 6 :
			output_filename = "outputHD.bmp";
			break;
		}
	}

	//the -S param
	temp = getArgParameter( WEIGHT_SCALE, argc, argv );
	if( temp != NULL )
	{
		weight_scale = atoi( temp );
	}
	else 
	{
		weight_scale = 100000;
	}

	//the -E param
	temp = getArgParameter( ENERGY_TYPE, argc, argv );
	if( temp != NULL )
	{
		ener = (CAIR_energy)atoi( temp );
	}
	else
	{
		ener = BACKWARD;
	}

	//the -T param
	temp = getArgParameter( THREAD_COUNT, argc, argv );
	if( temp != NULL )
	{
		CAIR_Threads( atoi(temp) );
	}

	
	//the -W param
	//set weights
	CML_int Weights( Image.TellWidth(), Image.TellHeight() );
	CML_int D_Weights( 1, 1 );
	Weights.Fill( 0 );
	//Weight_Rectangle( &Weights, 436, 411, 524, 604, -100000 ); //only useful for test.bmp
	//Weight_Rectangle( &Weights, 524, 487, 548, 552, -100000 );
	//Weight_Rectangle( &Weights, 532, 325, 1023, 468, 100000 );
	//Weight_Rectangle( &Weights, 551, 468, 1023, 544, 100000 );
	//Weight_Rectangle( &Weights, 605, 544, 1023, 588, 100000 );
	//Weight_Rectangle( &Weights, 624, 227, 1023, 325, 100000 );
	weight_filename = getArgParameter( WEIGHT_FILENAME, argc, argv );
	if( weight_filename != NULL )
	{
		BMP weight;
		weight.ReadFromFile( weight_filename );

		for( int x = 0; x < Image.TellWidth(); x++ )
		{
			for( int y = 0; y < Image.TellHeight(); y++ )
			{
				Weights(x,y) += weight_scale * (weight(x,y)->Green / 255);
				Weights(x,y) += -weight_scale * (weight(x,y)->Red / 255);
			}
		}
	}


	//resize and write out the image
	cout << "Please wait... ";
	start = clock ();

	BMP_to_CML( &Image, &Source );
	
	switch( result_type )
	{
	case 0 :
		CAIR( &Source, &Weights, goal_x, goal_y, convolution, ener, &D_Weights, &Dest, NULL ); //try cancel_callback
		break;
	case 1 :
		CAIR_Grayscale( &Source, &Dest );
		break;
	case 2 :
		CAIR_Edge( &Source, convolution, &Dest );
		break;
	case 3 :
		CAIR_V_Energy( &Source, convolution, ener, &Dest );
		break;
	case 4 :
		CAIR_H_Energy( &Source, convolution, ener, &Dest );
		break;
	case 5 :
		CAIR_Removal( &Source, &Weights, AUTO, 1, convolution, ener, &D_Weights, &Dest, NULL );
		break;
	case 6 :
		CAIR_HD( &Source, &Weights, goal_x, goal_y, convolution, ener, &D_Weights, &Dest, NULL );
		break;
	}
		
	Resized.SetSize( Dest.Width(), Dest.Height() );
	CML_to_BMP( &Dest, &Resized );

	end=clock();    
	int timeInSec = (int)(end - start) / CLOCKS_PER_SEC;

	printf ("Took: %d.%03ld seconds.\n", timeInSec, (end - start) * 1000 / CLOCKS_PER_SEC-timeInSec*1000 );
	Resized.WriteToFile( output_filename );

	return 0;
}
