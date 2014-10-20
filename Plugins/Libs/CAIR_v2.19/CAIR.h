#ifndef CAIR_H
#define CAIR_H

//=========================================================================================================//
//CAIR - Content Aware Image Resizer
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
//This thing should hopefully perform the image resize method developed by Shai Avidan and Ariel Shamir.
//=========================================================================================================//

#include "CAIR_CML.h"

//=========================================================================================================//
//The default number of threads that will be used for Grayscale, Edge, and Add/Remove operations.
//Minimum of 2 required.
#define CAIR_NUM_THREADS 4

//=========================================================================================================//
//Set the number of threads that CAIR should use. Minimum of 2 required.
//WARNING: Never call this function while CAIR() is processing an image, otherwise bad things will happen!
//Best to set this only once, before any CAIR operations take place.
void CAIR_Threads( int thread_count );

//=========================================================================================================//
//The Great CAIR Frontend. This baby will retarget Source using S_Weights into the dimensions supplied by goal_x and goal_y into D_Weights and Dest.
//Weights allows for an area to be biased for removal/protection. A large positive value will protect a portion of the image,
//and a large negative value will remove it. Do not exceed the limits of int's, as this will cause an overflow. I would suggest
//a safe range of -2,000,000 to 2,000,000 (this is a maximum guideline, much smaller weights will work just as well for most images).
//Weights must be the same size as Source. D_Weights will contain the weights of Dest after the resize. Dest is the output,
//and as such has no constraints (its contents will be destroyed, just so you know). 
//The internal order is this: remove horizontal, remove vertical, add horizontal, add vertical.
//CAIR can use multiple convolution methods to determine the image energy. 
//Prewitt and Sobel are close to each other in results and represent the "traditional" edge detection.
//V_SQUARE and V1 can produce some of the better quality results, but may remove from large objects to do so. Do note that V_SQUARE
//produces much larger edge values, any may require larger weight values (by about an order of magnitude) for effective operation.
//Laplacian is a second-derivative operator, and can limit some artifacts while generating others.
//CAIR also can use the new improved energy algorithm called "forward energy." Removing seams can sometimes add energy back to the image
//by placing nearby edges directly next to each other. Forward energy can get around this by determining the future cost of a seam.
//Forward energy removes most serious artifacts from a retarget, but is slightly more costly in terms of performance.
enum CAIR_convolution { PREWITT = 0, V1 = 1, V_SQUARE = 2, SOBEL = 3, LAPLACIAN = 4 };
enum CAIR_energy { BACKWARD = 0, FORWARD = 1 };
bool CAIR( CML_color * Source,
           CML_int * S_Weights,
           int goal_x,
           int goal_y,
           CAIR_convolution conv,
           CAIR_energy ener,
           CML_int * D_Weights,
           CML_color * Dest,
           bool (*CAIR_callback)(float) );

//=========================================================================================================//
//Simple function that generates the grayscale image of Source and places the result in Dest.
void CAIR_Grayscale( CML_color * Source, CML_color * Dest );

//=========================================================================================================//
//Simple function that generates the edge-detection image of Source and stores it in Dest.
void CAIR_Edge( CML_color * Source, CAIR_convolution conv, CML_color * Dest );

//=========================================================================================================//
//Simple function that generates the vertical energy map of Source placing it into Dest.
//All values are scaled down to their relative gray value. Weights are assumed all zero.
void CAIR_V_Energy( CML_color * Source, CAIR_convolution conv, CAIR_energy ener, CML_color * Dest );

//=========================================================================================================//
//Simple function that generates the horizontal energy map of Source placing it into Dest.
//All values are scaled down to their relative gray value. Weights are assumed all zero.
void CAIR_H_Energy( CML_color * Source, CAIR_convolution conv, CAIR_energy ener, CML_color * Dest );

//=========================================================================================================//
//Experimental
//Any area with a negative weight will be removed. This function has three modes, determined by the choice parameter.
//AUTO will have the function count the vertical and horizontal rows/columns and remove in the direction that has the least.
//VERTICAL will force the function to remove all negative weights in the vertical direction; likewise for HORIZONTAL.
//Because some conditions may cause the function not to remove all negative weights in one pass, max_attempts lets the function
//go through the removal process as many times as you're willing.
enum CAIR_direction { AUTO = 0, VERTICAL = 1, HORIZONTAL = 2 };
bool CAIR_Removal( CML_color * Source,
                   CML_int * S_Weights,
                   CAIR_direction choice,
                   int max_attempts,
                   CAIR_convolution conv,
                   CAIR_energy ener,
                   CML_int * D_Weights,
                   CML_color * Dest,
                   bool (*CAIR_callback)(float) );

//The following Image Map functions are deprecated until better alternatives can be made.
#if 0
//=========================================================================================================//
//Experimental
//Precompute removals in the x direction. Map will hold the largest width the corresponding pixel is still visible.
//This will calculate all removals down to 3 pixels in width.
//Right now this only performs removals and only the x-direction. For the future enlarging is planned. Precomputing for both directions
//doesn't work all that well and generates significant artifacts. This function is intended for "content-aware multi-size images" as mentioned
//in the doctor's presentation. The next logical step would be to encode Map into an existing image format. Then, using a function like
//CAIR_Map_Resize() the image can be resized on a client machine with very little overhead.
void CAIR_Image_Map( CML_color * Source, CML_int * Weights, CAIR_convolution conv, CAIR_energy ener, CML_int * Map );

//=========================================================================================================//
//Experimental
//An "example" function on how to decode the Map to quickly resize an image. This is only for the width, since multi-directional
//resizing produces significant artifacts. Do note this will produce different results than standard CAIR(), because this resize doesn't
//average pixels back into the image as does CAIR(). This function could be multi-threaded much like Remove_Path() for even faster performance.
void CAIR_Map_Resize( CML_color * Source, CML_int * Map, int goal_x, CML_color * Dest );
#endif

//=========================================================================================================//
//This works as CAIR, except here maximum quality is attempted. When removing in both directions some amount, CAIR_HD()
//will determine which direction has the least amount of energy and then removes in that direction. This is only done
//for removal, since enlarging will not benefit, although this function will perform addition just like CAIR().
//Inputs are the same as CAIR().
bool CAIR_HD( CML_color * Source,
              CML_int * S_Weights,
              int goal_x,
              int goal_y,
              CAIR_convolution conv,
              CAIR_energy ener,
              CML_int * D_Weights,
              CML_color * Dest,
              bool (*CAIR_callback)(float) );

#endif //CAIR_H
